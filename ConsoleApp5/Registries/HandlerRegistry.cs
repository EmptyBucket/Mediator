using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp5.Registries;

internal class HandlerRegistry : IHandlerRegistry, IHandlerProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<(Type, string?), List<IHandler>> _handlers = new();
    private readonly Dictionary<(Type, string?), List<Type>> _handlerTypes = new();

    public HandlerRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void AddHandler<TMessage>(IHandler handler, string? routingKey = null)
    {
        var key = (typeof(TMessage), routingKey);
        _lock.EnterWriteLock();
        _handlers.TryAdd(key, new List<IHandler>());
        _handlers[key].Add(handler);
        _lock.ExitWriteLock();
    }

    public void AddHandler<TMessage, THandler>(string? routingKey = null) where THandler : IHandler
    {
        var key = (typeof(TMessage), routingKey);
        _lock.EnterWriteLock();
        _handlerTypes.TryAdd(key, new List<Type>());
        _handlerTypes[key].Add(typeof(THandler));
        _lock.ExitWriteLock();
    }

    public void RemoveHandler<TMessage>(IHandler handler, string? routingKey = null)
    {
        var key = (typeof(TMessage), routingKey);
        _lock.EnterWriteLock();

        if (_handlers.TryGetValue(key, out var list))
        {
            list.Remove(handler);

            if (!list.Any()) _handlers.Remove(key);
        }

        _lock.ExitWriteLock();
    }

    public void RemoveHandler<TMessage, THandler>(string? routingKey = null) where THandler : IHandler
    {
        var key = (typeof(TMessage), routingKey);
        _lock.EnterWriteLock();

        if (_handlerTypes.TryGetValue(key, out var list))
        {
            list.Remove(typeof(THandler));

            if (!list.Any()) _handlers.Remove(key);
        }

        _lock.ExitWriteLock();
    }

    public IReadOnlyCollection<IHandler> GetHandlers<TMessage>(string? routingKey = null)
    {
        var key = (typeof(TMessage), routingKey);

        _lock.EnterReadLock();
        if (_handlers.TryGetValue(key, out var handlers))
            return handlers.ToArray();

        if (_handlerTypes.TryGetValue(key, out var handlerTypes))
            return handlerTypes.Select(t => _serviceProvider.GetRequiredService(t)).Cast<IHandler>().ToArray();
        _lock.ExitReadLock();

        return Array.Empty<IHandler>();
    }
}