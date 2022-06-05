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

    public IDisposable ConnectOut<TMessage>(IPubPipe pipe, string routingKey = "", string subscriptionId = "")
    {
        var route = Route.For<TMessage>(routingKey);
        CreateConsumerGroup(route, subscriptionId);
        var pipeConnection = ConnectPipe<TMessage>(route, pipe, subscriptionId);
        return pipeConnection;
    }

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default)
    {
        var route = Route.For<TMessage>(routingKey);
        await CreateConsumerGroupAsync(route, subscriptionId);
        var pipeConnection = ConnectPipe<TMessage>(route, pipe, subscriptionId);
        return pipeConnection;
    }

    private PipeConnection<IPubPipe> ConnectPipe<TMessage>(Route route, IPubPipe pipe, string subscriptionId)
    {
        var cts = new CancellationTokenSource();
        Task.Run(async () =>
        {
            await HandleAsync<TMessage>(route, pipe, subscriptionId, "0");

            while (!cts.Token.IsCancellationRequested)
            {
                await HandleAsync<TMessage>(route, pipe, subscriptionId, ">");
                await Task.Delay(100, cts.Token).ConfigureAwait(false);
            }
        }, cts.Token);
        var pipeConnection = new PipeConnection<IPubPipe>(route, pipe, DisconnectPipe);
        _pipeConnections.TryAdd(pipeConnection, cts);
        return pipeConnection;
    }

    private void DisconnectPipe(PipeConnection<IPubPipe> pipeConnection)
    {
        if (!_pipeConnections.TryRemove(pipeConnection, out var cts)) return;

        cts.Cancel();
        cts.Dispose();
    }

    private async Task HandleAsync<TMessage>(Route route, IPubPipe pipe, string subscriptionId, string position)
    {
        StreamEntry[] entries;

        do
        {
            entries = await _database
                .StreamReadGroupAsync(route.ToString(), subscriptionId, "", position, 1_000)
                .ConfigureAwait(false);

            foreach (var entry in entries)
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var ctx = JsonSerializer.Deserialize<MessageContext<TMessage>>(entry[WellKnown.MessageKey])!;
                ctx = ctx with { DeliveredAt = DateTimeOffset.Now, ServiceProvider = scope.ServiceProvider };
                await pipe.PassAsync(ctx);
                await _database
                    .StreamAcknowledgeAsync(route.ToString(), subscriptionId, entry.Id)
                    .ConfigureAwait(false);
            }
        } while (entries.Any());
    }

    private static string ConsumerGroupExistsExceptionMessage => "BUSYGROUP Consumer Group name already exists";

    private void CreateConsumerGroup(Route route, string subscriptionId)
    {
        try
        {
            _database.StreamCreateConsumerGroup(route.ToString(), subscriptionId, "0");
        }
        catch (RedisException e)
        {
            if (e.Message != ConsumerGroupExistsExceptionMessage) throw;
        }
    }

    private async Task CreateConsumerGroupAsync(Route route, string subscriptionId)
    {
        try
        {
            await _database.StreamCreateConsumerGroupAsync(route.ToString(), subscriptionId, "0").ConfigureAwait(false);
        }
        catch (RedisException e)
        {
            if (e.Message != ConsumerGroupExistsExceptionMessage) throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Keys) await pipeConnection.DisposeAsync();
    }
}