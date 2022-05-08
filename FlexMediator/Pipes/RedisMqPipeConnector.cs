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

using System.Text.Json;
using FlexMediator.Utils;
using StackExchange.Redis;

namespace FlexMediator.Pipes;

public class RedisMqPipeConnector : IPipeConnector
{
    private readonly ISubscriber _subscriber;
    private readonly Dictionary<Route, PipeConnection> _pipeConnections = new();

    public RedisMqPipeConnector(ISubscriber subscriber)
    {
        _subscriber = subscriber;
    }

    public async Task<PipeConnection> Out<TMessage>(IPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = new Route(typeof(TMessage), routingKey);

        if (_pipeConnections.TryGetValue(route, out var pipeConnection)) return pipeConnection;

        var channelMessageQueue = await _subscriber.SubscribeAsync(route.ToString());
        channelMessageQueue.OnMessage(async r =>
        {
            var message = JsonSerializer.Deserialize<TMessage>(r.ToString())!;
            await pipe.Handle<TMessage>(message, new MessageOptions(routingKey), token);
        });
        return _pipeConnections[route] =
            new PipeConnection(p => Disconnect(p, () => _subscriber.UnsubscribeAsync(route.ToString())), route, pipe);
    }

    public async Task<PipeConnection> Out<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = new Route(typeof(TMessage), routingKey);

        if (_pipeConnections.TryGetValue(route, out var pipeConnection)) return pipeConnection;

        var channelMessageQueue = await _subscriber.SubscribeAsync(route.ToString());
        channelMessageQueue.OnMessage(async r =>
        {
            var correlationId = JsonDocument.Parse(r.ToString()).RootElement.GetProperty("CorrelationId").ToString();

            try
            {
                var request = JsonSerializer.Deserialize<RedisMessage<TMessage>>(r.ToString());
                var result =
                    await pipe.Handle<TMessage, TResult>(request.Value!, new MessageOptions(routingKey), token);
                var response = new RedisMessage<TResult>(correlationId, result);
                await _subscriber.PublishAsync("responses", new RedisValue(JsonSerializer.Serialize(response)));
            }
            catch (Exception e)
            {
                var response = new RedisMessage<TResult>(correlationId, Exception: e.Message);
                await _subscriber.PublishAsync("responses", new RedisValue(JsonSerializer.Serialize(response)));
                throw;
            }
        });
        return _pipeConnections[route] =
            new PipeConnection(p => Disconnect(p, () => _subscriber.UnsubscribeAsync(route.ToString())), route, pipe);
    }

    private async ValueTask Disconnect(PipeConnection pipeConnection, Func<Task> unsubscribe)
    {
        if (_pipeConnections.Remove(pipeConnection.Route)) await unsubscribe.Invoke();
    }
}