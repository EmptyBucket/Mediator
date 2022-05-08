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

    public async Task<PipeConnection> Into<TMessage>(IPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = Route.For<TMessage>(routingKey);

        if (_pipeConnections.TryGetValue(route, out var pipeConnection)) return pipeConnection;

        var subscription = await _bus.PubSub.SubscribeAsync<TMessage>(string.Empty,
            (m, c) => pipe.Handle(m, new MessageOptions(routingKey), c),
            c => c.WithQueueName(route).WithTopic(route), token);
        return _pipeConnections[route] = new PipeConnection(route, pipe, p => Disconnect(p, subscription));
    }

    public async Task<PipeConnection> Into<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>(routingKey);

        if (_pipeConnections.TryGetValue(route, out var pipeConnection)) return pipeConnection;

        var subscription = await _bus.Rpc.RespondAsync<TMessage, TResult>(
            (m, c) => pipe.Handle<TMessage, TResult>(m, new MessageOptions(routingKey), c),
            c => c.WithQueueName(route), token); 
        return _pipeConnections[route] = new PipeConnection(route, pipe, p => Disconnect(p, subscription));
    }

    private ValueTask Disconnect(PipeConnection pipeConnection, IDisposable subscription)
    {
        if (_pipeConnections.Remove(pipeConnection.Route)) subscription.Dispose();

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Values) await pipeConnection.DisposeAsync();
    }
}