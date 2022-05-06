using EasyNetQ;
using FlexMediator.Utils;

namespace FlexMediator.Pipes;

public class RabbitMqPipeConnector : IPipeConnector
{
    private readonly IBus _bus;
    private readonly Dictionary<Route, IDisposable> _subscriptions = new();

    public RabbitMqPipeConnector(IBus bus)
    {
        _bus = bus;
    }

    public async Task<PipeConnection> Connect<TMessage>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        if (!_subscriptions.ContainsKey(route))
        {
            var subscription = await _bus.PubSub.SubscribeAsync<TMessage>(routingKey,
                (m, c) => pipe.Handle(m, new MessageOptions(routingKey), c),
                c => c.WithDurable(false).WithAutoDelete().WithTopic(route.ToString()));
            _subscriptions[route] = subscription;
        }

        return new PipeConnection(Disconnect, route, pipe);
    }

    public async Task<PipeConnection> Connect<TMessage, TResult>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey, typeof(TResult));

        if (!_subscriptions.ContainsKey(route))
        {
            var subscription = await _bus.Rpc.RespondAsync<TMessage, TResult>(
                (m, c) => pipe.Handle<TMessage, TResult>(m, new MessageOptions(routingKey), c),
                c => c.WithDurable(false).WithQueueName(route.ToString()));
            _subscriptions[route] = subscription;
        }

        return new PipeConnection(Disconnect, route, pipe);
    }

    private ValueTask Disconnect(PipeConnection pipeConnection)
    {
        if (_subscriptions.Remove(pipeConnection.Route, out var topology)) topology.Dispose();

        return ValueTask.CompletedTask;
    }
}