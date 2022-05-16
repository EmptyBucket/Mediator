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
using Mediator.Pipes.PublishSubscribe;
using Mediator.Pipes.RequestResponse;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Mediator.Redis.Pipes;

public class RedisMqPipeConnector : IPipeConnector
{
    private readonly ISubscriber _subscriber;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<PipeConnection, PipeConnection> _pipeConnections = new();

    public RedisMqPipeConnector(IConnectionMultiplexer connectionMultiplexer, IServiceProvider serviceProvider)
    {
        _subscriber = connectionMultiplexer.GetSubscriber();
        _serviceProvider = serviceProvider;
    }

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default)
    {
        async Task Handle(ChannelMessage m)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var ctx = JsonSerializer.Deserialize<MessageContext<TMessage>>(m.Message)!;
            ctx = ctx with { DeliveredAt = DateTimeOffset.Now, ServiceProvider = scope.ServiceProvider };
            await pipe.PassAsync(ctx, token);
        }

        var route = Route.For<TMessage>(routingKey);
        var channelMq = await _subscriber.SubscribeAsync(route.ToString()).ConfigureAwait(false);
        channelMq.OnMessage(Handle);
        var pipeConnection = new PipeConnection(channelMq, p => _pipeConnections.TryRemove(p, out _));
        _pipeConnections.TryAdd(pipeConnection, pipeConnection);
        return pipeConnection;
    }

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        async Task Handle(ChannelMessage m)
        {
            var messageCtx = JsonSerializer.Deserialize<MessageContext<TMessage>>(m.Message)!;

            try
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                messageCtx =
                    messageCtx with { DeliveredAt = DateTimeOffset.Now, ServiceProvider = scope.ServiceProvider };
                var result = await pipe.PassAsync<TMessage, TResult>(messageCtx, token);
                var resultCtx = new ResultContext<TResult>(messageCtx.Route, Guid.NewGuid().ToString(),
                    messageCtx.CorrelationId, DateTimeOffset.Now) { Result = result };
                await _subscriber
                    .PublishAsync(RedisWellKnown.ResponseMq, JsonSerializer.Serialize(resultCtx))
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                var resultCtx = new ResultContext<TResult>(messageCtx.Route, Guid.NewGuid().ToString(),
                    messageCtx.CorrelationId, DateTimeOffset.Now) { Exception = e };
                await _subscriber
                    .PublishAsync(RedisWellKnown.ResponseMq, JsonSerializer.Serialize(resultCtx))
                    .ConfigureAwait(false);
                throw;
            }
        }

        var route = Route.For<TMessage, TResult>(routingKey);
        var channelMq = await _subscriber.SubscribeAsync(route.ToString()).ConfigureAwait(false);
        channelMq.OnMessage(Handle);
        var pipeConnection = new PipeConnection(channelMq, p => _pipeConnections.TryRemove(p, out _));
        _pipeConnections.TryAdd(pipeConnection, pipeConnection);
        return pipeConnection;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Values) await pipeConnection.DisposeAsync();
    }

    private class PipeConnection : IAsyncDisposable
    {
        private readonly ChannelMessageQueue _channelMessageQueue;
        private readonly Action<PipeConnection> _callback;

        public PipeConnection(ChannelMessageQueue channelMessageQueue, Action<PipeConnection> callback)
        {
            _channelMessageQueue = channelMessageQueue;
            _callback = callback;
        }

        public async ValueTask DisposeAsync()
        {
            await _channelMessageQueue.UnsubscribeAsync();
            _callback(this);
        }
    }
}