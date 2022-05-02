using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp5.Registries;

internal class RabbitMqHandlerRegistry : IHandlerRegistry
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IBus _bus;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<RegistryKey, IDisposable> _subscriptions = new();

    public RabbitMqHandlerRegistry(IServiceScopeFactory serviceScopeFactory, IBus bus)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _bus = bus;
    }

    public async Task AddHandler<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var key = new RegistryKey(typeof(TMessage), routingKey, Handler: handler);

        _lock.EnterWriteLock();

        try
        {
            if (!_subscriptions.ContainsKey(key))
            {
                var subscription = await _bus.PubSub.SubscribeAsync<TMessage>(routingKey,
                    (m, c) => handler.Handle(m!, new MessageOptions(routingKey), c),
                    c => c.WithDurable(false).WithAutoDelete());
                _subscriptions[key] = subscription;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public async Task AddHandler<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "")
    {
        var key = new RegistryKey(typeof(TMessage), routingKey, Handler: handler);

        _lock.EnterWriteLock();

        try
        {
            if (!_subscriptions.ContainsKey(key))
            {
                var subscription = await _bus.Rpc.RespondAsync<TMessage, TResult>(
                    (m, c) => handler.Handle(m!, new MessageOptions(routingKey), c),
                    c => c.WithDurable(false));
                _subscriptions[key] = subscription;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public async Task AddHandler<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var key = new RegistryKey(typeof(TMessage), routingKey, HandlerType: typeof(THandler));

        _lock.EnterWriteLock();

        try
        {
            if (!_subscriptions.ContainsKey(key))
            {
                var subscription = await _bus.PubSub.SubscribeAsync<TMessage>(routingKey, async (m, c) =>
                    {
                        using var serviceScope = _serviceScopeFactory.CreateScope();
                        var handler = serviceScope.ServiceProvider.GetRequiredService<THandler>();
                        await handler.Handle(m!, new MessageOptions(routingKey), c);
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

    public async Task AddHandler<TMessage, THandler, TResult>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>
    {
        var key = new RegistryKey(typeof(TMessage), routingKey, HandlerType: typeof(THandler));

        _lock.EnterWriteLock();

        try
        {
            if (!_subscriptions.ContainsKey(key))
            {
                var subscription = await _bus.Rpc.RespondAsync<TMessage, TResult>(async (m, c) =>
                    {
                        using var serviceScope = _serviceScopeFactory.CreateScope();
                        var handler = serviceScope.ServiceProvider.GetRequiredService<THandler>();
                        return await handler.Handle(m!, new MessageOptions(routingKey), c);
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

    public Task RemoveHandler<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var key = new RegistryKey(typeof(TMessage), routingKey, Handler: handler);

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

    public Task RemoveHandler<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var key = new RegistryKey(typeof(TMessage), routingKey, HandlerType: typeof(THandler));

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

    private record RegistryKey(Type MessageType, string RoutingKey, IHandler? Handler = null, Type? HandlerType = null);
}