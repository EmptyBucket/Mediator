// MIT License
// 
// Copyright (c) 2022 Alexey Politov
// https://github.com/EmptyBucket/Mediator
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.Utils;
using Mediator.Utils;
using Microsoft.Extensions.DependencyInjection;
using NATS.Net;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using System.Collections.Concurrent;
using Mediator.Nats.Utils;

namespace Mediator.Nats.Pipes;

public class NatsStreamPipe : IMulticastPubPipe
{
    private int _isDisposed;
    private readonly ConcurrentDictionary<string, byte> _knownStreams = new();
    private readonly ConcurrentDictionary<string, byte> _knownConsumers = new();
    private readonly INatsConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly INatsJSContext _jetStream;
    private readonly Func<StreamConfig> _streamConfigFactory;
    private readonly Func<ConsumerConfig> _consumerConfigFactory;
    private readonly ConcurrentDictionary<PipeConnection<IPubPipe>, (CancellationTokenSource Cts, Task Task)>
        _pipeConnections = new();

    public NatsStreamPipe(INatsConnection connection, IServiceProvider serviceProvider, NatsJSOpts? jsOpts = null,
        Func<StreamConfig>? streamConfigFactory = null, Func<ConsumerConfig>? consumerConfigFactory = null)
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        _jetStream = _connection.CreateJetStreamContext(jsOpts ?? new NatsJSOpts(connection.Opts));
        _streamConfigFactory = streamConfigFactory ?? (() => new StreamConfig());
        _consumerConfigFactory = consumerConfigFactory ?? (() => new ConsumerConfig());
    }

    /// <inheritdoc />
    public async Task PassAsync<TMessage>(MessageContext<TMessage> ctx, CancellationToken cancellationToken = default)
    {
        await _connection.ConnectAsync().ConfigureAwait(false);
        var streamName = ctx.Route.ToStreamName();
        await EnsureStreamAsync(streamName, cancellationToken).ConfigureAwait(false);
        var natsJsPubOpts = new PropertyBinder<NatsJSPubOpts>().Bind(ctx.ServiceProps).Build();
        await _jetStream.PublishAsync<SerializableMessageContext<TMessage>>(streamName, ctx, opts: natsJsPubOpts,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IEnumerable<PipeConnection<IPubPipe>> GetPubConnections() => _pipeConnections.Keys;

    /// <inheritdoc />
    public PipeConnection<IPubPipe> ConnectOut<TMessage>(IPubPipe pipe, string routingKey = "",
        string connectionName = "", string subscriptionId = "default")
    {
        _connection.ConnectAsync().GetAwaiter().GetResult();
        var route = Route.For<TMessage>(routingKey);
        var streamName = route.ToStreamName();
        EnsureConsumerAsync(streamName, subscriptionId, CancellationToken.None).GetAwaiter().GetResult();
        var pipeConnection = ConnectPipe<TMessage>(connectionName, route, pipe, streamName, subscriptionId);
        return pipeConnection;
    }

    /// <inheritdoc />
    public async Task<PipeConnection<IPubPipe>> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string connectionName = "", string subscriptionId = "default", CancellationToken cancellationToken = default)
    {
        await _connection.ConnectAsync().ConfigureAwait(false);
        var route = Route.For<TMessage>(routingKey);
        var streamName = route.ToStreamName();
        await EnsureConsumerAsync(streamName, subscriptionId, cancellationToken).ConfigureAwait(false);
        var pipeConnection = ConnectPipe<TMessage>(connectionName, route, pipe, streamName, subscriptionId);
        return pipeConnection;
    }

    private PipeConnection<IPubPipe> ConnectPipe<TMessage>(string connectionName, Route route, IPubPipe pipe,
        string streamName, string consumerName)
    {
        var cts = new CancellationTokenSource();
        var task = HandleAsync<TMessage>(pipe, streamName, consumerName, cts.Token);
        var pipeConnection = new PipeConnection<IPubPipe>(connectionName, route, pipe, DisconnectPipe,
            DisconnectPipeAsync);
        _pipeConnections.TryAdd(pipeConnection, (cts, task));
        return pipeConnection;
    }

    private void DisconnectPipe(PipeConnection<IPubPipe> pipeConnection) =>
        DisconnectPipeAsync(pipeConnection).AsTask().GetAwaiter().GetResult();

    private async ValueTask DisconnectPipeAsync(PipeConnection<IPubPipe> pipeConnection)
    {
        if (!_pipeConnections.TryRemove(pipeConnection, out var tuple)) return;

        tuple.Cts.Cancel();

        try
        {
            await tuple.Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            tuple.Cts.Dispose();
        }
    }

    private async Task HandleAsync<TMessage>(IPubPipe pipe, string streamName, string consumerName,
        CancellationToken cancellationToken)
    {
        var consumer = await _jetStream.GetConsumerAsync(streamName, consumerName, cancellationToken)
            .ConfigureAwait(false);

        await foreach (var message in consumer.ConsumeAsync<SerializableMessageContext<TMessage>>(
                           cancellationToken: cancellationToken))
        {
            if (message.Data is { } data)
            {
                await using var serviceScope = _serviceProvider.CreateAsyncScope();
                var ctx = new MessageContext<TMessage>(data.Route, data.Message, data.ServiceProps, data.ExtraProps)
                    { DeliveredAt = DateTime.Now, ServiceProvider = serviceScope.ServiceProvider };
                await pipe.PassAsync(ctx, cancellationToken).ConfigureAwait(false);
            }

            await message.AckAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task EnsureStreamAsync(string streamName, CancellationToken cancellationToken)
    {
        if (!_knownStreams.TryAdd(streamName, 0)) return;

        var streamConfig = _streamConfigFactory() with { Name = streamName, Subjects = new[] { streamName } };
        await _jetStream.CreateOrUpdateStreamAsync(streamConfig, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureConsumerAsync(string streamName, string consumerName, CancellationToken cancellationToken)
    {
        if (!_knownConsumers.TryAdd(consumerName, 0)) return;

        await EnsureStreamAsync(streamName, cancellationToken);
        var consumerConfig = _consumerConfigFactory() with
        {
            Name = consumerName,
            DurableName = consumerName,
            AckPolicy = ConsumerConfigAckPolicy.Explicit
        };
        await _jetStream.CreateOrUpdateConsumerAsync(streamName, consumerConfig, cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) return;

        foreach (var pipeConnection in _pipeConnections.Keys)
            await pipeConnection.DisposeAsync();
    }
}