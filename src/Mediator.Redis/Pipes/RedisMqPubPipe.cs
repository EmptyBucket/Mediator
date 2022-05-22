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
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Mediator.Redis.Pipes;

internal class RedisMqPubPipe : IConnectingPubPipe
{
    private readonly ISubscriber _subscriber;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<PipeConnection<IPubPipe>, PipeConnection<IPubPipe>> _pipeConnections = new();

    public RedisMqPubPipe(IConnectionMultiplexer multiplexer, IServiceProvider serviceProvider)
    {
        _subscriber = multiplexer.GetSubscriber();
        _serviceProvider = serviceProvider;
    }

    public async Task PassAsync<TMessage>(MessageContext<TMessage> ctx, CancellationToken token = default) =>
        await _subscriber
            .PublishAsync(ctx.Route.ToString(), JsonSerializer.Serialize(ctx))
            .ConfigureAwait(false);

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default)
    {
        async Task HandleAsync(ChannelMessage m)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var ctx = JsonSerializer.Deserialize<MessageContext<TMessage>>(m.Message)!;
            ctx = ctx with { DeliveredAt = DateTimeOffset.Now, ServiceProvider = scope.ServiceProvider };
            await pipe.PassAsync(ctx, token);
        }

        var route = Route.For<TMessage>(routingKey);
        
        var channelMq = await _subscriber.SubscribeAsync(route.ToString()).ConfigureAwait(false);
        channelMq.OnMessage(HandleAsync);
        var pipeConnection = new PipeConnection<IPubPipe>(route, pipe, async p =>
        {
            if (_pipeConnections.TryRemove(p, out _)) await channelMq.UnsubscribeAsync();
        });
        _pipeConnections.TryAdd(pipeConnection, pipeConnection);
        return pipeConnection;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Values) await pipeConnection.DisposeAsync();
    }
}