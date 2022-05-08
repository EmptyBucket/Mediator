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
using StackExchange.Redis;

namespace FlexMediator.Pipes;

public class RedisMqPipe : IPipe, IPipeConnector
{
    private readonly ISubscriber _subscriber;
    private readonly IPipeConnector _pipeConnector;
    private readonly ConcurrentDictionary<string, Action<string>> _responseActions = new();

    public RedisMqPipe(ISubscriber subscriber)
    {
        _subscriber = subscriber;
        _pipeConnector = new RedisMqPipeConnector(subscriber);
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options,
        CancellationToken token = default)
    {
        var route = new Route(typeof(TMessage), options.RoutingKey);
        await _subscriber.PublishAsync(route.ToString(), new RedisValue(JsonSerializer.Serialize(message)));
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token = default)
    {
        await EnsureResponsesMqExist();

        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _responseActions.TryAdd(correlationId, r =>
        {
            var response = JsonSerializer.Deserialize<RedisMessage<TResult>>(r);

            if (response.Value is not null) tcs.SetResult(response.Value);
            else if (response.Exception is not null) tcs.SetException(new Exception(response.Exception));
            else tcs.SetException(new ArgumentException("Message was not be processed"));
        });

        try
        {
            var route = new Route(typeof(TMessage), options.RoutingKey, typeof(TResult));
            var request = new RedisMessage<TMessage>(correlationId, message);
            await _subscriber.PublishAsync(route.ToString(), new RedisValue(JsonSerializer.Serialize(request)));
            return await tcs.Task;
        }
        finally
        {
            _responseActions.TryRemove(correlationId, out _);
        }
    }

    private async Task EnsureResponsesMqExist()
    {
        if (!_subscriber.IsConnected("responses"))
            await _subscriber.SubscribeAsync("responses", (_, r) =>
            {
                var correlationId =
                    JsonDocument.Parse(r.ToString()).RootElement.GetProperty("CorrelationId").ToString();
                _responseActions[correlationId].Invoke(r);
            });
    }

    public async Task<PipeConnection> Out<TMessage>(IPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        await _pipeConnector.Out<TMessage>(pipe, routingKey, token);

    public async Task<PipeConnection> Out<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        await _pipeConnector.Out<TMessage, TResult>(pipe, routingKey, token);
}

internal readonly record struct RedisMessage<T>(string CorrelationId, T? Value = default, string? Exception = null);