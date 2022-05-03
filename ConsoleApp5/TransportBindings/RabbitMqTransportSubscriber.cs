using ConsoleApp5.Models;
using ConsoleApp5.Pipes;
using EasyNetQ;

namespace ConsoleApp5.TransportBindings;

internal class RabbitMqTransportSubscriber : ITransportSubscriber
{
    private readonly IBus _bus;
    private readonly IPipe _receivePipe;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<Route, IDisposable> _subscriptions = new();

    public RabbitMqTransportSubscriber(IBus bus, IPipe receivePipe)
    {
        _bus = bus;
        _receivePipe = receivePipe;
    }

    public async Task Subscribe<TMessage>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (!_subscriptions.ContainsKey(route))
            {
                var subscription = await _bus.PubSub.SubscribeAsync<TMessage>(routingKey,
                    (m, c) => _receivePipe.Handle(m, new MessageOptions(routingKey), c),
                    c => c.WithDurable(false).WithAutoDelete());
                _subscriptions[route] = subscription;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public async Task Subscribe<TMessage, TResult>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (!_subscriptions.ContainsKey(route))
            {
                var subscription = await _bus.Rpc.RespondAsync<TMessage, TResult>(
                    (m, c) => _receivePipe.Handle<TMessage, TResult>(m, new MessageOptions(routingKey), c),
                    c => c.WithDurable(false));
                _subscriptions[route] = subscription;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task Unsubscribe<TMessage>(string routingKey = "")
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