using EasyNetQ;
using FlexMediator.Utils;

namespace FlexMediator.Pipes;

public class RabbitMqPipeConnector : IPipeConnector
{
    private readonly IBus _bus;
    private readonly Dictionary<Route, PipeConnection> _pipeConnections = new();

    public RabbitMqPipeConnector(IBus bus)
    {
        _bus = bus;
    }

    public async Task<PipeConnection> Out<TMessage>(IPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = new Route(typeof(TMessage), routingKey);

        if (_pipeConnections.TryGetValue(route, out var pipeConnection)) return pipeConnection;

        var subscription = await _bus.PubSub.SubscribeAsync<TMessage>(string.Empty,
            (m, c) => pipe.Handle(m, new MessageOptions(routingKey), c),
            c => c.WithQueueName(route).WithTopic(route), token);
        return _pipeConnections[route] = new PipeConnection(p => Disconnect(p, subscription), route, pipe);
    }

    public async Task<PipeConnection> Out<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = new Route(typeof(TMessage), routingKey, typeof(TResult));

        if (_pipeConnections.TryGetValue(route, out var pipeConnection)) return pipeConnection;

        var subscription = await _bus.Rpc.RespondAsync<TMessage, TResult>(
            (m, c) => pipe.Handle<TMessage, TResult>(m, new MessageOptions(routingKey), c),
            c => c.WithQueueName(route), token);
        return _pipeConnections[route] = new PipeConnection(p => Disconnect(p, subscription), route, pipe);
    }

    private ValueTask Disconnect(PipeConnection pipeConnection, IDisposable subscription)
    {
        if (_pipeConnections.Remove(pipeConnection.Route)) subscription.Dispose();

        return ValueTask.CompletedTask;
    }
}