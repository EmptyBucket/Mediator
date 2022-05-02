using ConsoleApp5.Transports;
using EasyNetQ;

namespace ConsoleApp5.Bindings;

internal class RabbitMqBindingRegistry : IBindingRegistry
{
    private readonly IBus _bus;
    private readonly Transport _transport;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<Binding, IDisposable> _subscriptions = new();

    public RabbitMqBindingRegistry(IBus bus, Transport transport)
    {
        _bus = bus;
        _transport = transport;
    }

    public async Task Add<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var key = new Binding<TMessage>(routingKey, handler: handler);

        _lock.EnterWriteLock();

        try
        {
            await _transport.Bindings.Add(handler, routingKey);

            if (!_subscriptions.ContainsKey(key))
            {
                var subscription = await _bus.PubSub.SubscribeAsync<TMessage>(routingKey,
                    (m, c) => _transport.Pipe.Handle(m, new MessageOptions(routingKey), c),
                    c => c.WithDurable(false).WithAutoDelete());
                _subscriptions[key] = subscription;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public async Task Add<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "")
    {
        var key = new Binding<TMessage>(routingKey, handler: handler);

        _lock.EnterWriteLock();

        try
        {
            await _transport.Bindings.Add(handler, routingKey);

            if (!_subscriptions.ContainsKey(key))
            {
                var subscription = await _bus.Rpc.RespondAsync<TMessage, TResult>(
                    (m, c) => _transport.Pipe.Handle<TMessage, TResult>(m, new MessageOptions(routingKey), c),
                    c => c.WithDurable(false));
                _subscriptions[key] = subscription;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public async Task Add<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var key = new Binding<TMessage>(routingKey, handlerType: typeof(THandler));

        _lock.EnterWriteLock();

        try
        {
            await _transport.Bindings.Add<TMessage, THandler>(routingKey);

            if (!_subscriptions.ContainsKey(key))
            {
                var subscription = await _bus.PubSub.SubscribeAsync<TMessage>(routingKey,
                    (m, c) => _transport.Pipe.Handle(m, new MessageOptions(routingKey), c),
                    c => c.WithDurable(false).WithAutoDelete());
                _subscriptions[key] = subscription;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public async Task Add<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>
    {
        var key = new Binding<TMessage>(routingKey, handlerType: typeof(THandler));

        _lock.EnterWriteLock();

        try
        {
            await _transport.Bindings.Add<TMessage, TResult, THandler>(routingKey);

            if (!_subscriptions.ContainsKey(key))
            {
                var subscription = await _bus.Rpc.RespondAsync<TMessage, TResult>(
                    (m, c) => _transport.Pipe.Handle<TMessage, TResult>(m, new MessageOptions(routingKey), c),
                    c => c.WithDurable(false));
                _subscriptions[key] = subscription;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public async Task Remove<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var key = new Binding<TMessage>(routingKey, handler: handler);

        _lock.EnterWriteLock();

        try
        {
            await _transport.Bindings.Remove(handler, routingKey);

            if (_subscriptions.Remove(key, out var subscription)) subscription.Dispose();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public async Task Remove<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var key = new Binding<TMessage>(routingKey, handlerType: typeof(THandler));

        _lock.EnterWriteLock();

        try
        {
            await _transport.Bindings.Remove<TMessage, THandler>(routingKey);

            if (_subscriptions.Remove(key, out var subscription)) subscription.Dispose();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}