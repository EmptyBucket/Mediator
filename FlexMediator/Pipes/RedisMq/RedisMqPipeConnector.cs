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
using FlexMediator.Utils;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace FlexMediator.Pipes.RedisMq;

public class RedisMqPipeConnector : IPipeConnector
{
    private readonly ISubscriber _subscriber;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<PipeConnection, PipeConnection> _pipeConnections = new();

    public RedisMqPipeConnector(ISubscriber subscriber, IServiceProvider serviceProvider)
    {
        _subscriber = subscriber;
        _serviceProvider = serviceProvider;
    }

    public async Task<PipeConnection> ConnectOutAsync<TMessage>(IPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default)
    {
        var route = Route.For<TMessage>(routingKey);
        var channelMq = await _subscriber.SubscribeAsync(route.ToString()).ConfigureAwait(false);
        channelMq.OnMessage(async r =>
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var message = JsonSerializer.Deserialize<TMessage>(r.Message)!;
            var messageContext = new MessageContext(scope.ServiceProvider, routingKey);
            await pipe.PassAsync<TMessage>(message, messageContext, token);
        });
        var pipeConnection = new PipeConnection(route, pipe, p => Disconnect(p, () => channelMq.UnsubscribeAsync()));
        Connect(pipeConnection);
        return pipeConnection;
    }

    public async Task<PipeConnection> ConnectOutAsync<TMessage, TResult>(IPipe pipe, string routingKey = "", CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>(routingKey);
        var channelMq = await _subscriber.SubscribeAsync(route.ToString()).ConfigureAwait(false);
        channelMq.OnMessage(async r =>
        {
            var request = JsonSerializer.Deserialize<RedisMqMessage<TMessage>>(r.Message);

            try
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var messageContext = new MessageContext(scope.ServiceProvider, routingKey);
                var result = await pipe.PassAsync<TMessage, TResult>(request.Value!, messageContext, token);
                var response = new RedisMqMessage<TResult>(request.CorrelationId, result);
                await _subscriber.PublishAsync(RedisMqWellKnown.ResponsesMq, JsonSerializer.Serialize(response))
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                var response = new RedisMqMessage<TResult>(request.CorrelationId, Exception: e.Message);
                await _subscriber.PublishAsync(RedisMqWellKnown.ResponsesMq, JsonSerializer.Serialize(response))
                    .ConfigureAwait(false);
                throw;
            }
        });
        var pipeConnection = new PipeConnection(route, pipe, p => Disconnect(p, () => channelMq.UnsubscribeAsync()));
        Connect(pipeConnection);
        return pipeConnection;
    }

    private void Connect(PipeConnection pipeConnection) => _pipeConnections.TryAdd(pipeConnection, pipeConnection);

    private ValueTask Disconnect(PipeConnection pipeConnection, Func<Task> unsubscribe)
    {
        if (_pipeConnections.TryRemove(pipeConnection, out _)) unsubscribe();

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Values) await pipeConnection.DisposeAsync();
    }
}