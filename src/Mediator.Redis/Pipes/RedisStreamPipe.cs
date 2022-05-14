using System.Text.Json;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.PublishSubscribe;
using StackExchange.Redis;

namespace Mediator.Redis.Pipes;

public class RedisStreamPipe : IConnectingPubPipe
{
    private readonly IDatabase _database;
    private readonly RedisStreamPipeConnector _redisStreamPipeConnector;

    public RedisStreamPipe(IConnectionMultiplexer connectionMultiplexer, IServiceProvider serviceProvider)
    {
        _database = connectionMultiplexer.GetDatabase();
        _redisStreamPipeConnector = new RedisStreamPipeConnector(connectionMultiplexer, serviceProvider);
    }

    public async Task PassAsync<TMessage>(TMessage message, MessageContext context, CancellationToken token = default)
    {
        var route = Route.For<TMessage>(context.RoutingKey);
        await _database
            .StreamAddAsync(route.ToString(), RedisWellKnown.MessageKey, JsonSerializer.Serialize(message))
            .ConfigureAwait(false);
    }

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default) =>
        _redisStreamPipeConnector.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionId, token);

    public ValueTask DisposeAsync() => _redisStreamPipeConnector.DisposeAsync();
}