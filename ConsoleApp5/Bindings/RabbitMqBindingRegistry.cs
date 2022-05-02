using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp5.Bindings;

internal class RabbitMqBindingRegistry : IBindingRegistry
{
    private readonly IBus _bus;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<Binding, IDisposable> _subscriptions = new();

    public RabbitMqBindingRegistry(IBus bus, IServiceScopeFactory serviceScopeFactory)
    {
        _bus = bus;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task Add<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var key = new Binding<TMessage>(routingKey, handler: handler);

        _lock.EnterWriteLock();

        try
        {
            if (!_subscriptions.ContainsKey(key))
            {
                var subscription = await _bus.PubSub.SubscribeAsync<TMessage>(routingKey,
                    (m, c) => handler.HandleAsync(m!, new MessageOptions(routingKey), c),
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
            if (!_subscriptions.ContainsKey(key))
            {
                var subscription = await _bus.Rpc.RespondAsync<TMessage, TResult>(
                    (m, c) => handler.HandleAsync(m!, new MessageOptions(routingKey), c),
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
            if (!_subscriptions.ContainsKey(key))
            {
                var subscription = await _bus.PubSub.SubscribeAsync<TMessage>(routingKey, async (m, c) =>
                    {
                        using var serviceScope = _serviceScopeFactory.CreateScope();
                        var handler = serviceScope.ServiceProvider.GetRequiredService<THandler>();
                        await handler.HandleAsync(m!, new MessageOptions(routingKey), c);
                    },
                    c => c.WithDurable(false).WithAutoDelete());
                _subscriptions[key] = subscription;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public async Task Add<TMessage, THandler, TResult>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>
    {
        var key = new Binding<TMessage>(routingKey, handlerType: typeof(THandler));

        _lock.EnterWriteLock();

        try
        {
            if (!_subscriptions.ContainsKey(key))
            {
                var subscription = await _bus.Rpc.RespondAsync<TMessage, TResult>(async (m, c) =>
                    {
                        using var serviceScope = _serviceScopeFactory.CreateScope();
                        var handler = serviceScope.ServiceProvider.GetRequiredService<THandler>();
                        return await handler.HandleAsync(m!, new MessageOptions(routingKey), c);
                    },
                    c => c.WithDurable(false));
                _subscriptions[key] = subscription;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task Remove<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var key = new Binding<TMessage>(routingKey, handler: handler);

        _lock.EnterWriteLock();

        try
        {
            if (_subscriptions.Remove(key, out var subscription)) subscription.Dispose();
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
        var key = new Binding<TMessage>(routingKey, handlerType: typeof(THandler));

        _lock.EnterWriteLock();

        try
        {
            if (_subscriptions.Remove(key, out var subscription)) subscription.Dispose();
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }
}