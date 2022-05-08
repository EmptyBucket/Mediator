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

    public async Task<PipeConnection> Out<TMessage>(IPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = new Route(typeof(TMessage), routingKey);

        if (_pipeConnections.TryGetValue(route, out var pipeConnection)) return pipeConnection;

        await _subscriber.SubscribeAsync(route.ToString(), (_, r) =>
        {
            var message = JsonSerializer.Deserialize<TMessage>(r.ToString())!;
            pipe.Handle<TMessage>(message, new MessageOptions(routingKey), CancellationToken.None);
        });
        return _pipeConnections[route] =
            new PipeConnection(p => Disconnect(p, () => _subscriber.UnsubscribeAsync(route.ToString())), route, pipe);
    }

    public async Task<PipeConnection> Out<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = new Route(typeof(TMessage), routingKey);

        if (_pipeConnections.TryGetValue(route, out var pipeConnection)) return pipeConnection;

        await _subscriber.SubscribeAsync(route.ToString(), (_, r) =>
        {
            var correlationId = JsonDocument.Parse(r.ToString()).RootElement.GetProperty("CorrelationId").ToString();

            try
            {
                var request = JsonSerializer.Deserialize<RedisMessage<TMessage>>(r.ToString());
                var result = pipe
                    .Handle<TMessage, TResult>(request.Value!, new MessageOptions(routingKey), CancellationToken.None)
                    .Result;
                var response = new RedisMessage<TResult>(correlationId, result);
                _subscriber.PublishAsync("responses", new RedisValue(JsonSerializer.Serialize(response)));
            }
            catch (Exception e)
            {
                var response = new RedisMessage<TResult>(correlationId, Exception: e.Message);
                _subscriber.PublishAsync("responses", new RedisValue(JsonSerializer.Serialize(response)));
                throw;
            }
        });

        return _pipeConnections[route] =
            new PipeConnection(p => Disconnect(p, () => _subscriber.UnsubscribeAsync(route.ToString())), route, pipe);
    }

    private async ValueTask Disconnect(PipeConnection pipeConnection, Func<Task> unsubscribe)
    {
        if (_pipeConnections.Remove(pipeConnection.Route)) await unsubscribe.Invoke();
    }
}