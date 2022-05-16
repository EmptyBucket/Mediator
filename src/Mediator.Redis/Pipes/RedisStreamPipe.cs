using System.Text.Json;
using Mediator.Handlers;
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

    public async Task PassAsync<TMessage>(MessageContext<TMessage> ctx, CancellationToken token = default) =>
        await _database
            .StreamAddAsync(ctx.Route.ToString(), RedisWellKnown.MessageKey, JsonSerializer.Serialize(ctx))
            .ConfigureAwait(false);

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default) =>
        _redisStreamPipeConnector.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionId, token);

    public ValueTask DisposeAsync() => _redisStreamPipeConnector.DisposeAsync();
}