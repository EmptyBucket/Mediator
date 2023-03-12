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

using System.Collections.Concurrent;
using System.Text.Json;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.Utils;
using Mediator.Redis.Utils;
using Mediator.Utils;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Mediator.Redis.Pipes;

internal class RedisMqPubPipe : IConnectingPubPipe
{
    private int _isDisposed;
    private readonly ISubscriber _subscriber;
    private readonly IServiceProvider _serviceProvider;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly ConcurrentDictionary<PipeConnection<IPubPipe>, ChannelMessageQueue> _pipeConnections = new();

    public RedisMqPubPipe(IConnectionMultiplexer multiplexer, IServiceProvider serviceProvider)
    {
        _subscriber = multiplexer.GetSubscriber();
        _serviceProvider = serviceProvider;
        _jsonSerializerOptions = new JsonSerializerOptions { Converters = { ObjectToInferredTypesConverter.Instance } };
    }

    public async Task PassAsync<TMessage>(MessageContext<TMessage> ctx, CancellationToken cancellationToken = default)
    {
        var message = JsonSerializer.Serialize(ctx, _jsonSerializerOptions);
        var messageProperties = new PropertyBinder<MessageProperties>().Bind(ctx.ServiceProps).Build();
        await _subscriber.PublishAsync(ctx.Route.ToString(), message, messageProperties.Flags).ConfigureAwait(false);
    }

    public IDisposable ConnectOut<TMessage>(IPubPipe pipe, string routingKey = "", string subscriptionId = "")
    {
        var route = Route.For<TMessage>(routingKey);
        var pipeConnection = ConnectPipe<TMessage>(route, pipe);
        return pipeConnection;
    }

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken cancellationToken = default)
    {
        var route = Route.For<TMessage>(routingKey);
        var pipeConnection = await ConnectPipeAsync<TMessage>(route, pipe);
        return pipeConnection;
    }

    private PipeConnection<IPubPipe> ConnectPipe<TMessage>(Route route, IPubPipe pipe)
    {
        var channelMq = _subscriber.Subscribe(route.ToString());
        channelMq.OnMessage(m => HandleAsync<TMessage>(pipe, m));
        var pipeConnection = new PipeConnection<IPubPipe>(route, pipe, DisconnectPipe, DisconnectPipeAsync);
        _pipeConnections.TryAdd(pipeConnection, channelMq);
        return pipeConnection;
    }

    private async Task<PipeConnection<IPubPipe>> ConnectPipeAsync<TMessage>(Route route, IPubPipe pipe)
    {
        var channelMq = await _subscriber.SubscribeAsync(route.ToString()).ConfigureAwait(false);
        channelMq.OnMessage(m => HandleAsync<TMessage>(pipe, m));
        var pipeConnection = new PipeConnection<IPubPipe>(route, pipe, DisconnectPipe, DisconnectPipeAsync);
        _pipeConnections.TryAdd(pipeConnection, channelMq);
        return pipeConnection;
    }

    private void DisconnectPipe(PipeConnection<IPubPipe> pipeConnection)
    {
        if (_pipeConnections.TryRemove(pipeConnection, out var c)) c.Unsubscribe();
    }

    private async ValueTask DisconnectPipeAsync(PipeConnection<IPubPipe> pipeConnection)
    {
        if (_pipeConnections.TryRemove(pipeConnection, out var c)) await c.UnsubscribeAsync().ConfigureAwait(false);
    }

    private async Task HandleAsync<TMessage>(IPubPipe pipe, ChannelMessage channelMessage)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var ctx = JsonSerializer.Deserialize<MessageContext<TMessage>>(channelMessage.Message, _jsonSerializerOptions)!;
        ctx = ctx with { DeliveredAt = DateTime.Now, ServiceProvider = scope.ServiceProvider };
        await pipe.PassAsync(ctx);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) return;

        foreach (var pipeConnection in _pipeConnections.Keys) await pipeConnection.DisposeAsync();
    }
}