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
                var ctx = JsonSerializer.Deserialize<MessageContext<TMessage>>(entry[RedisWellKnown.MessageKey])!;
                ctx = ctx with { DeliveredAt = DateTimeOffset.Now, ServiceProvider = scope.ServiceProvider };
                await pipe.PassAsync(ctx, c);
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