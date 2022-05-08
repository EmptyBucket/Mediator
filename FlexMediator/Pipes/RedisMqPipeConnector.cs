using System.Text.Json;
using FlexMediator.Utils;
using StackExchange.Redis;

namespace FlexMediator.Pipes;

public class RedisMqPipeConnector : IPipeConnector
{
    private readonly ISubscriber _subscriber;
    private readonly Dictionary<Route, PipeConnection> _pipeConnections = new();

    public RedisMqPipeConnector(ISubscriber subscriber)
    {
        _subscriber = subscriber;
    }

    public async Task<PipeConnection> Into<TMessage>(IPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = Route.For<TMessage>(routingKey);

        if (_pipeConnections.TryGetValue(route, out var pipeConnection)) return pipeConnection;

        var channelMessageQueue = await _subscriber.SubscribeAsync(route.ToString());
        channelMessageQueue.OnMessage(async r =>
        {
            var message = JsonSerializer.Deserialize<TMessage>(r.Message)!;
            await pipe.Handle<TMessage>(message, new MessageOptions(routingKey), token);
        });
        return _pipeConnections[route] =
            new PipeConnection(route, pipe, p => Disconnect(p, () => _subscriber.UnsubscribeAsync(route.ToString())));
    }

    public async Task<PipeConnection> Into<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>(routingKey);

        if (_pipeConnections.TryGetValue(route, out var pipeConnection)) return pipeConnection;

        var channelMessageQueue = await _subscriber.SubscribeAsync(route.ToString());
        channelMessageQueue.OnMessage(async r =>
        {
            var request = JsonSerializer.Deserialize<RedisMessage<TMessage>>(r.Message);

            try
            {
                var result =
                    await pipe.Handle<TMessage, TResult>(request.Value!, new MessageOptions(routingKey), token);
                var response = new RedisMessage<TResult>(request.CorrelationId, result);
                await _subscriber.PublishAsync("responses", new RedisValue(JsonSerializer.Serialize(response)));
            }
            catch (Exception e)
            {
                var response = new RedisMessage<TResult>(request.CorrelationId, Exception: e.Message);
                await _subscriber.PublishAsync("responses", new RedisValue(JsonSerializer.Serialize(response)));
                throw;
            }
        });
        return _pipeConnections[route] =
            new PipeConnection(route, pipe, p => Disconnect(p, () => _subscriber.UnsubscribeAsync(route.ToString())));
    }

    private async ValueTask Disconnect(PipeConnection pipeConnection, Func<Task> unsubscribe)
    {
        if (_pipeConnections.Remove(pipeConnection.Route)) await unsubscribe.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Values) await pipeConnection.DisposeAsync();
    }
}