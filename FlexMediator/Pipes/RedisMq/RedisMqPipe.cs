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

namespace FlexMediator.Pipes.RedisMq;

public class RedisMqPipe : IPipe, IPipeConnector
{
    private int _isDisposed;
    private readonly ISubscriber _subscriber;
    private readonly IPipeConnector _pipeConnector;
    private readonly Lazy<Task<ChannelMessageQueue>> _responsesMq;
    private readonly ConcurrentDictionary<string, Action<string>> _responseActions = new();

    public RedisMqPipe(ISubscriber subscriber, IServiceProvider serviceProvider)
    {
        _subscriber = subscriber;
        _pipeConnector = new RedisMqPipeConnector(subscriber, serviceProvider);
        _responsesMq = new Lazy<Task<ChannelMessageQueue>>(async () =>
        {
            var channelMq = await _subscriber.SubscribeAsync(RedisMqWellKnown.ResponsesMq).ConfigureAwait(false);
            channelMq.OnMessage(r =>
            {
                var message = r.Message.ToString();
                var jsonElement = JsonDocument.Parse(message).RootElement;
                var correlationId = jsonElement.GetProperty(nameof(RedisMqMessage<int>.CorrelationId)).ToString();
                if (_responseActions.TryRemove(correlationId, out var action)) action(message);
            });
            return channelMq;
        });
    }

    public async Task PassAsync<TMessage>(TMessage message, MessageContext context,
        CancellationToken token = default)
    {
        var route = Route.For<TMessage>(context.RoutingKey);
        await _subscriber.PublishAsync(route.ToString(), JsonSerializer.Serialize(message)).ConfigureAwait(false);
    }

    public async Task<TResult> PassAsync<TMessage, TResult>(TMessage message, MessageContext context,
        CancellationToken token = default)
    {
        await _responsesMq.Value;

        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _responseActions.TryAdd(correlationId, r =>
        {
            var response = JsonSerializer.Deserialize<RedisMqMessage<TResult>>(r);

            if (response.Value is not null) tcs.SetResult(response.Value);
            else if (response.Exception is not null) tcs.SetException(new Exception(response.Exception));
            else tcs.SetException(new InvalidOperationException("Message was not be processed"));
        });

        var route = Route.For<TMessage, TResult>(context.RoutingKey);
        var request = new RedisMqMessage<TMessage>(correlationId, message);
        await _subscriber.PublishAsync(route.ToString(), JsonSerializer.Serialize(request)).ConfigureAwait(false);
        var result = await tcs.Task.ConfigureAwait(false);
        return result;
    }

    public async Task<PipeConnection> ConnectOutAsync<TMessage>(IPipe pipe, string routingKey = "",
        string subscriptionName = "", CancellationToken token = default) =>
        await _pipeConnector.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionName, token);

    public async Task<PipeConnection> ConnectOutAsync<TMessage, TResult>(IPipe pipe, string routingKey = "",
        string subscriptionName = "", CancellationToken token = default) =>
        await _pipeConnector.ConnectOutAsync<TMessage, TResult>(pipe, routingKey, subscriptionName, token);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1) return;

        if (_responsesMq.IsValueCreated) await _responsesMq.Value.Result.UnsubscribeAsync().ConfigureAwait(false);

        await _pipeConnector.DisposeAsync();
    }
}