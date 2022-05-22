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
    private readonly ConcurrentDictionary<PipeConnection<IPubPipe>, PipeConnection<IPubPipe>> _pipeConnections = new();

    public RedisStreamPipe(IConnectionMultiplexer multiplexer, IServiceProvider serviceProvider)
    {
        _database = multiplexer.GetDatabase();
        _serviceProvider = serviceProvider;
    }
    
    public async Task PassAsync<TMessage>(MessageContext<TMessage> ctx, CancellationToken token = default) =>
        await _database
            .StreamAddAsync(ctx.Route.ToString(), RedisWellKnown.MessageKey, JsonSerializer.Serialize(ctx))
            .ConfigureAwait(false);

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default)
    {
        async Task Handle(Route r, string position, CancellationToken c)
        {
            var entries = await _database
                .StreamReadGroupAsync(r.ToString(), subscriptionId, "", position)
                .ConfigureAwait(false);

            foreach (var entry in entries)
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var ctx = JsonSerializer.Deserialize<MessageContext<TMessage>>(entry[RedisWellKnown.MessageKey])!;
                ctx = ctx with { DeliveredAt = DateTimeOffset.Now, ServiceProvider = scope.ServiceProvider };
                await pipe.PassAsync(ctx, c);
                await _database.StreamAcknowledgeAsync(r.ToString(), subscriptionId, entry.Id);
            }
        }

        var route = Route.For<TMessage>(routingKey);
        
        await CreateConsumerGroup(route, subscriptionId);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        await Handle(route, "0", cts.Token);
        Task.Run(async () =>
        {
            var spinWait = new SpinWait();

            while (!cts.Token.IsCancellationRequested)
            {
                await Handle(route, ">", cts.Token);
                spinWait.SpinOnce();
            }
        }, cts.Token);
        var pipeConnection = new PipeConnection<IPubPipe>(route, pipe, p =>
        {
            if (_pipeConnections.TryRemove(p, out _))
            {
                cts.Cancel();
                cts.Dispose();
            }

            return ValueTask.CompletedTask;
        });
        _pipeConnections.TryAdd(pipeConnection, pipeConnection);
        return pipeConnection;
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
        foreach (var pipeConnection in _pipeConnections.Values) await pipeConnection.DisposeAsync();
    }
}