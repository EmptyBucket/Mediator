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
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Mediator.Redis.Pipes;

public class RedisStreamPipeConnector : IPubPipeConnector
{
    private readonly IDatabase _database;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<PipeConnection, PipeConnection> _pipeConnections = new();

    public RedisStreamPipeConnector(IConnectionMultiplexer connectionMultiplexer, IServiceProvider serviceProvider)
    {
        _database = connectionMultiplexer.GetDatabase();
        _serviceProvider = serviceProvider;
    }

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
                var message = JsonSerializer.Deserialize<TMessage>(entry[RedisWellKnown.MessageKey]);
                await pipe.PassAsync(message, new MessageContext(scope.ServiceProvider, routingKey), c);
                await _database.StreamAcknowledgeAsync(r.ToString(), subscriptionId, entry.Id);
            }
        }

        var route = Route.For<TMessage>(routingKey);
        await CreateConsumerGroup(route, subscriptionId);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        token = cts.Token;
        await Handle(route, "0", token);
        Task.Run(async () =>
        {
            var spinWait = new SpinWait();

            while (!token.IsCancellationRequested)
            {
                await Handle(route, ">", token);
                spinWait.SpinOnce();
            }
        }, token);
        var pipeConnection = new PipeConnection(cts, p => _pipeConnections.TryRemove(p, out _));
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

    private class PipeConnection : IAsyncDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly Action<PipeConnection> _callback;

        public PipeConnection(CancellationTokenSource cts, Action<PipeConnection> callback)
        {
            _cts = cts;
            _callback = callback;
        }

        public ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _cts.Dispose();
            _callback(this);
            return ValueTask.CompletedTask;
        }
    }
}