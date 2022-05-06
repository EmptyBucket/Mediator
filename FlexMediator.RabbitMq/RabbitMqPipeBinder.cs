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

    public async Task<PipeBind> Bind<TMessage>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        if (!_subscriptions.ContainsKey(route))
        {
            var subscription = await _bus.PubSub.SubscribeAsync<TMessage>(routingKey,
                (m, c) => pipe.Handle(m, new MessageOptions(routingKey), c),
                c => c.WithDurable(false).WithAutoDelete().WithTopic(route.ToString()));
            _subscriptions[route] = subscription;
        }

        return new PipeBind(Unbind, route, pipe);
    }

    public async Task<PipeBind> Bind<TMessage, TResult>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey, typeof(TResult));

        if (!_subscriptions.ContainsKey(route))
        {
            var subscription = await _bus.Rpc.RespondAsync<TMessage, TResult>(
                (m, c) => pipe.Handle<TMessage, TResult>(m, new MessageOptions(routingKey), c),
                c => c.WithDurable(false).WithQueueName(route.ToString()));
            _subscriptions[route] = subscription;
        }

        return new PipeBind(Unbind, route, pipe);
    }

    private ValueTask Unbind(PipeBind pipeBind)
    {
        if (_subscriptions.Remove(pipeBind.Route, out var topology)) topology.Dispose();

        return ValueTask.CompletedTask;
    }
}