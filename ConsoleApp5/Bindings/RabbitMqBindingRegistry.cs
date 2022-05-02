using ConsoleApp5.Pipes;
using EasyNetQ;

namespace ConsoleApp5.Bindings;

internal class RabbitMqBindingRegistry : IBindingRegistry
{
    private readonly IBus _bus;
    private readonly IPipe _receivePipe;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<Binding, IDisposable> _subscriptions = new();

    public RabbitMqBindingRegistry(IBus bus, IPipe receivePipe)
    {
        _bus = bus;
        _receivePipe = receivePipe;
    }

    public async Task Add<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var key = new Binding(new Route(typeof(TMessage), routingKey), handler: handler);

        _lock.EnterWriteLock();

        try
        {
            if (!_subscriptions.ContainsKey(key))
            {
                var subscription = await _bus.PubSub.SubscribeAsync<TMessage>(routingKey,
                    (m, c) => _receivePipe.Handle(m, new MessageOptions(routingKey), c),
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
        var key = new Binding(new Route(typeof(TMessage), routingKey), handler: handler);

        _lock.EnterWriteLock();

        try
        {
            if (!_subscriptions.ContainsKey(key))
            {
                var subscription = await _bus.Rpc.RespondAsync<TMessage, TResult>(
                    (m, c) => _receivePipe.Handle<TMessage, TResult>(m, new MessageOptions(routingKey), c),
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
        var key = new Binding(new Route(typeof(TMessage), routingKey), handlerType: typeof(THandler));

        _lock.EnterWriteLock();

        try
        {
            if (!_subscriptions.ContainsKey(key))
            {
                var subscription = await _bus.PubSub.SubscribeAsync<TMessage>(routingKey,
                    (m, c) => _receivePipe.Handle(m, new MessageOptions(routingKey), c),
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
        var binding = new Binding(new Route(typeof(TMessage), routingKey), handlerType: typeof(THandler));

        _lock.EnterWriteLock();

        try
        {
            if (!_subscriptions.ContainsKey(binding))
            {
                var subscription = await _bus.Rpc.RespondAsync<TMessage, TResult>(
                    (m, c) => _receivePipe.Handle<TMessage, TResult>(m, new MessageOptions(routingKey), c),
                    c => c.WithDurable(false));
                _subscriptions[binding] = subscription;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task Remove<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var binding = new Binding(new Route(typeof(TMessage), routingKey), handler: handler);

        _lock.EnterWriteLock();

        try
        {
            if (_subscriptions.Remove(binding, out var subscription)) subscription.Dispose();
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task Remove<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var binding = new Binding(new Route(typeof(TMessage), routingKey), handlerType: typeof(THandler));

        _lock.EnterWriteLock();

        try
        {
            if (_subscriptions.Remove(binding, out var subscription)) subscription.Dispose();
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }
}