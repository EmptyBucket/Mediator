namespace ConsoleApp5.Registries;

internal class MemoryHandlerRegistry : IHandlerRegistry
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<RegistryKey, HashSet<RegistryEntry>> _handlers = new();

    public Task AddHandler<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var key = new RegistryKey(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _handlers.TryAdd(key, new HashSet<RegistryEntry>());
            _handlers[key].Add(new RegistryEntry(Handler: handler));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task AddHandler<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "")
    {
        var key = new RegistryKey(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _handlers.TryAdd(key, new HashSet<RegistryEntry>());
            _handlers[key].Add(new RegistryEntry(Handler: handler));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task AddHandler<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var key = new RegistryKey(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _handlers.TryAdd(key, new HashSet<RegistryEntry>());
            _handlers[key].Add(new RegistryEntry(HandlerType: typeof(THandler)));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task AddHandler<TMessage, THandler, TResult>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>
    {
        var key = new RegistryKey(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _handlers.TryAdd(key, new HashSet<RegistryEntry>());
            _handlers[key].Add(new RegistryEntry(HandlerType: typeof(THandler)));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task RemoveHandler<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var key = new RegistryKey(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (_handlers.TryGetValue(key, out var set))
            {
                set.Remove(new RegistryEntry(Handler: handler));

                if (!set.Any()) _handlers.Remove(key);
            }
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
        var key = new RegistryKey(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (_handlers.TryGetValue(key, out var set))
            {
                set.Remove(new RegistryEntry(HandlerType: typeof(THandler)));

                if (!set.Any()) _handlers.Remove(key);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    private record RegistryKey(Type MessageType, string RoutingKey = "");

    private record RegistryEntry(IHandler? Handler = null, Type? HandlerType = null);
}