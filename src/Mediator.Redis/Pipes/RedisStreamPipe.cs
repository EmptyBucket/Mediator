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