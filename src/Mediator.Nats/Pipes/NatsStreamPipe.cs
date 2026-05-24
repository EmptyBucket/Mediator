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
using System.Text.Json;
using Mediator.Nats.Utils;
using NATS.Client.Serializers.Json;

namespace Mediator.Nats.Pipes;

public class NatsStreamPipe : IMulticastPubPipe
{
    private int _isDisposed;
    private readonly INatsConnection _natsConnection;
    private readonly INatsJSContext _jetStream;
    private readonly IServiceProvider _serviceProvider;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly Func<StreamConfig> _streamConfigFactory;
    private readonly Func<ConsumerConfig> _configureConfigFactory;
    private readonly NatsJSConsumeOpts _consumeOpts;
    private readonly ConcurrentDictionary<PipeConnection<IPubPipe>, (CancellationTokenSource Cts, Task Task)>
        _pipeConnections = new();

    public NatsStreamPipe(INatsConnection natsConnection, IServiceProvider serviceProvider,
        JsonSerializerOptions? jsonSerializerOptions = null, Func<StreamConfig>? streamConfigFactory = null,
        Func<ConsumerConfig>? configureConfigFactory = null, NatsJSConsumeOpts? consumeOpts = null)
    {
        _natsConnection = natsConnection;
        _jetStream = _natsConnection.CreateJetStreamContext();
        _serviceProvider = serviceProvider;
        _jsonSerializerOptions = jsonSerializerOptions ?? new JsonSerializerOptions
            { Converters = { ObjectToInferredTypesConverter.Instance } };
        _streamConfigFactory = streamConfigFactory ?? (() => new StreamConfig());
        _configureConfigFactory = configureConfigFactory ?? (() => new ConsumerConfig());
        _consumeOpts = consumeOpts ?? new NatsJSConsumeOpts { MaxMsgs = 1_000 };
    }

    /// <inheritdoc />
    public async Task PassAsync<TMessage>(MessageContext<TMessage> ctx, CancellationToken cancellationToken = default)
    {
        await _natsConnection.ConnectAsync();
        var serializer = new NatsJsonSerializer<MessageContext<TMessage>>(_jsonSerializerOptions);
        var natsJsPubOpts = new PropertyBinder<NatsJSPubOpts>().Bind(ctx.ServiceProps).Build();
        await _jetStream.PublishAsync(ctx.Route.ToStreamName(), ctx, serializer, natsJsPubOpts,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IEnumerable<PipeConnection<IPubPipe>> GetPubConnections() => _pipeConnections.Keys;

    /// <inheritdoc />
    public PipeConnection<IPubPipe> ConnectOut<TMessage>(IPubPipe pipe, string routingKey = "",
        string connectionName = "", string subscriptionId = "default")
    {
        _natsConnection.ConnectAsync().GetAwaiter().GetResult();
        var route = Route.For<TMessage>(routingKey);
        var streamName = route.ToStreamName();
        EnsureStreamAndConsumerAsync(streamName, subscriptionId, CancellationToken.None).GetAwaiter().GetResult();
        var pipeConnection = ConnectPipe<TMessage>(connectionName, route, pipe, streamName, subscriptionId);
        return pipeConnection;
    }

    /// <inheritdoc />
    public async Task<PipeConnection<IPubPipe>> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string connectionName = "", string subscriptionId = "default", CancellationToken cancellationToken = default)
    {
        await _natsConnection.ConnectAsync();
        var route = Route.For<TMessage>(routingKey);
        var streamName = route.ToStreamName();
        await EnsureStreamAndConsumerAsync(streamName, subscriptionId, cancellationToken);
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

    private void DisconnectPipe(PipeConnection<IPubPipe> c) => DisconnectPipeAsync(c).AsTask().GetAwaiter().GetResult();

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
        var serializer = new NatsJsonSerializer<MessageContext<TMessage>>(_jsonSerializerOptions);
        var consumer = await _jetStream.GetConsumerAsync(streamName, consumerName, cancellationToken);

        await foreach (var message in consumer.ConsumeAsync(serializer, _consumeOpts,
                           cancellationToken: cancellationToken))
        {
            await using var scope = _serviceProvider.CreateAsyncScope();

            if (message.Data != null)
            {
                var ctx = message.Data with { DeliveredAt = DateTime.Now, ServiceProvider = scope.ServiceProvider };
                await pipe.PassAsync(ctx, cancellationToken);
            }

            await message.AckAsync(cancellationToken: cancellationToken);
        }
    }

    private async Task EnsureStreamAndConsumerAsync(string streamName, string consumerName,
        CancellationToken cancellationToken)
    {
        await EnsureStreamAsync(streamName, cancellationToken).ConfigureAwait(false);
        await EnsureConsumerAsync(streamName, consumerName, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureStreamAsync(string streamName, CancellationToken cancellationToken)
    {
        var streamConfig = _streamConfigFactory() with { Name = streamName, Subjects = new[] { streamName } };
        await _jetStream.CreateOrUpdateStreamAsync(streamConfig, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureConsumerAsync(string streamName, string consumerName, CancellationToken cancellationToken)
    {
        var consumerConfig = _configureConfigFactory() with
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