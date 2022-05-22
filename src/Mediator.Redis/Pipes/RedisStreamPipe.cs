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

public class RedisStreamPipe : IConnectingPubPipe
{
    private readonly IDatabase _database;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<PipeConnection<IPubPipe>, CancellationTokenSource> _pipeConnections = new();

    public RedisStreamPipe(IConnectionMultiplexer multiplexer, IServiceProvider serviceProvider)
    {
        _database = multiplexer.GetDatabase();
        _serviceProvider = serviceProvider;
    }

    public async Task PassAsync<TMessage>(MessageContext<TMessage> ctx, CancellationToken token = default)
    {
        var redisValue = JsonSerializer.Serialize(ctx);
        await _database.StreamAddAsync(ctx.Route.ToString(), WellKnown.MessageKey, redisValue).ConfigureAwait(false);
    }

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default)
    {
        var route = Route.For<TMessage>(routingKey);
        await CreateConsumerGroup(route, subscriptionId);
        var pipeConnection = await ConnectPipe<TMessage>(route, pipe, subscriptionId);
        return pipeConnection;
    }

    private async Task<IAsyncDisposable> ConnectPipe<TMessage>(Route route, IPubPipe pipe, string subscriptionId)
    {
        async Task HandleAsync(Route r, string p)
        {
            var entries = await _database
                .StreamReadGroupAsync(r.ToString(), subscriptionId, "", p)
                .ConfigureAwait(false);

            foreach (var entry in entries)
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var ctx = JsonSerializer.Deserialize<MessageContext<TMessage>>(entry[WellKnown.MessageKey])!;
                ctx = ctx with { DeliveredAt = DateTimeOffset.Now, ServiceProvider = scope.ServiceProvider };
                await pipe.PassAsync(ctx);
                await _database.StreamAcknowledgeAsync(r.ToString(), subscriptionId, entry.Id);
            }
        }

        var cts = new CancellationTokenSource();
        await HandleAsync(route, "0");
        Task.Run(async () =>
        {
            for (var spinWait = new SpinWait(); !cts.Token.IsCancellationRequested; spinWait.SpinOnce())
                await HandleAsync(route, ">");
        }, cts.Token);
        var pipeConnection = new PipeConnection<IPubPipe>(route, pipe, DisconnectPipe);
        _pipeConnections.TryAdd(pipeConnection, cts);
        return pipeConnection;
    }

    private ValueTask DisconnectPipe(PipeConnection<IPubPipe> p)
    {
        if (_pipeConnections.TryRemove(p, out var c))
        {
            c.Cancel();
            c.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    private async Task CreateConsumerGroup(Route route, string subscriptionId)
    {
        try
        {
            await _database.StreamCreateConsumerGroupAsync(route.ToString(), subscriptionId, "0").ConfigureAwait(false);
        }
        catch (RedisException e)
        {
            if (e.Message != "BUSYGROUP Consumer Group name already exists") throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Keys) await pipeConnection.DisposeAsync();
    }
}