using ConsoleApp5.Pipes;
using EasyNetQ;

namespace ConsoleApp5.Bindings;

internal class RabbitMqPipeBinder : IPipeBinder
{
    private readonly IBus _bus;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<Route, IDisposable> _subscriptions = new();

    public RabbitMqPipeBinder(IBus bus)
    {
        _bus = bus;
    }

    public async Task Bind<TMessage>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (!_subscriptions.ContainsKey(route))
            {
                var subscription = await _bus.PubSub.SubscribeAsync<TMessage>(routingKey,
                    (m, c) => pipe.Handle(m, new MessageOptions(routingKey), c),
                    c => c.WithDurable(false).WithAutoDelete());
                _subscriptions[route] = subscription;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public async Task Bind<TMessage, TResult>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (!_subscriptions.ContainsKey(route))
            {
                var subscription = await _bus.Rpc.RespondAsync<TMessage, TResult>(
                    (m, c) => pipe.Handle<TMessage, TResult>(m, new MessageOptions(routingKey), c),
                    c => c.WithDurable(false));
                _subscriptions[route] = subscription;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task Unbind<TMessage>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (_subscriptions.Remove(route, out var topology)) topology.Dispose();
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }
}