using System.Collections.Concurrent;
using System.Text.Json;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.Utils;
using Mediator.Redis.Utils;
using Mediator.Utils;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Mediator.Redis.Pipes;

public class RedisStreamPubPipe : IConnectingPubPipe
{
    private readonly IDatabase _database;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<PipeConnection<IPubPipe>, CancellationTokenSource> _pipeConnections = new();
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public RedisStreamPubPipe(IConnectionMultiplexer multiplexer, IServiceProvider serviceProvider)
    {
        _database = multiplexer.GetDatabase();
        _serviceProvider = serviceProvider;
        _jsonSerializerOptions = new JsonSerializerOptions { Converters = { ObjectToInferredTypesConverter.Instance } };
    }

    public async Task PassAsync<TMessage>(MessageContext<TMessage> ctx, CancellationToken token = default)
    {
        var message = JsonSerializer.Serialize(ctx, _jsonSerializerOptions);
        var messageProperties = new PropertyBinder<MessageProperties>().Bind(ctx.ServiceProps).Build();
        await _database.StreamAddAsync(ctx.Route.ToString(), WellKnown.MessageKey, message,
                messageProperties.MessageId, messageProperties.MaxLenght, messageProperties.UseApproximateMaxLength,
                messageProperties.Flags)
            .ConfigureAwait(false);
    }

    public IDisposable ConnectOut<TMessage>(IPubPipe pipe, string routingKey = "", string subscriptionId = "")
    {
        var route = Route.For<TMessage>(routingKey);
        var (groupName, consumerName) = ParseSubscriptionId(subscriptionId);
        _database.TryCreateConsumerGroup(route, groupName, "0");
        var pipeConnection = ConnectPipe<TMessage>(route, pipe, groupName, consumerName);
        return pipeConnection;
    }

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default)
    {
        var route = Route.For<TMessage>(routingKey);
        var (groupName, consumerName) = ParseSubscriptionId(subscriptionId);
        await _database.TryCreateConsumerGroupAsync(route, groupName, "0");
        var pipeConnection = ConnectPipe<TMessage>(route, pipe, groupName, consumerName);
        return pipeConnection;
    }

    private PipeConnection<IPubPipe> ConnectPipe<TMessage>(Route route, IPubPipe pipe, string groupName,
        string consumerName)
    {
        var cts = new CancellationTokenSource();
        Task.Run(async () =>
        {
            const int batchSize = 1_000;
            const int millisecondsDelay = 100;

            await HandleAsync<TMessage>(route, pipe, groupName, consumerName, "0", batchSize);

            while (!cts.Token.IsCancellationRequested)
            {
                await HandleAsync<TMessage>(route, pipe, groupName, consumerName, ">", batchSize);
                await Task.Delay(millisecondsDelay, cts.Token).ConfigureAwait(false);
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

    private async Task HandleAsync<TMessage>(Route route, IPubPipe pipe, string groupName, string consumerName,
        string position, int count)
    {
        StreamEntry[] entries;

        do
        {
            entries = await _database
                .StreamReadGroupAsync(route.ToString(), groupName, consumerName, position, count)
                .ConfigureAwait(false);
            var now = DateTime.Now;

            foreach (var entry in entries)
            {
                var ctx = JsonSerializer.Deserialize<MessageContext<TMessage>>(entry[WellKnown.MessageKey],
                    _jsonSerializerOptions)!;
                await using var scope = _serviceProvider.CreateAsyncScope();
                ctx = ctx with { MessageId = entry.Id, DeliveredAt = now, ServiceProvider = scope.ServiceProvider };
                await pipe.PassAsync(ctx);
                await _database.StreamAcknowledgeAsync(route.ToString(), groupName, entry.Id).ConfigureAwait(false);
            }
        } while (entries.Any());
    }

    private static (string groupName, string consumerName) ParseSubscriptionId(string subscriptionId) =>
        subscriptionId.IndexOf(':', StringComparison.Ordinal) is var idx && idx >= 0
            ? (subscriptionId[..idx], subscriptionId[(idx + 1)..])
            : (subscriptionId, string.Empty);

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Keys) await pipeConnection.DisposeAsync();
    }
}