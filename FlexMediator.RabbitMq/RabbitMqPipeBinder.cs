using EasyNetQ;
using FlexMediator;
using FlexMediator.Pipes;

namespace FlexMediatorRabbit;

internal class RabbitMqPipeBinder : IPipeBinder
{
    private readonly IBus _bus;
    private readonly Dictionary<Route, IDisposable> _subscriptions = new();

    public RabbitMqPipeBinder(IBus bus)
    {
        _bus = bus;
    }

    public async Task Bind<TMessage>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), RoutingKey: routingKey);

        if (!_subscriptions.ContainsKey(route))
        {
            var subscription = await _bus.PubSub.SubscribeAsync<TMessage>(routingKey,
                (m, c) => pipe.Handle(m, new MessageOptions(routingKey), c),
                c => c.WithDurable(false).WithAutoDelete().WithTopic(route.ToString()));
            _subscriptions[route] = subscription;
        }
    }

    public async Task Bind<TMessage, TResult>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), ResultType: typeof(TResult), RoutingKey: routingKey);

        if (!_subscriptions.ContainsKey(route))
        {
            var subscription = await _bus.Rpc.RespondAsync<TMessage, TResult>(
                (m, c) => pipe.Handle<TMessage, TResult>(m, new MessageOptions(routingKey), c),
                c => c.WithDurable(false).WithQueueName(route.ToString()));
            _subscriptions[route] = subscription;
        }
    }

    public Task Unbind<TMessage>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), RoutingKey: routingKey);

        if (_subscriptions.Remove(route, out var topology)) topology.Dispose();

        return Task.CompletedTask;
    }

    public Task Unbind<TMessage, TResult>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), ResultType: typeof(TResult), RoutingKey: routingKey);

        if (_subscriptions.Remove(route, out var topology)) topology.Dispose();

        return Task.CompletedTask;
    }
}