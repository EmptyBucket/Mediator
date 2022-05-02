namespace ConsoleApp5.Bindings;

internal class MemoryBindingRegistry : IBindingRegistry, IBindingProvider
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<Route, HashSet<Binding>> _bindings = new();

    public Task Add<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var key = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _bindings.TryAdd(key, new HashSet<Binding>());
            _bindings[key].Add(new Binding<TMessage>(routingKey, handler: handler));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task Add<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "")
    {
        var key = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _bindings.TryAdd(key, new HashSet<Binding>());
            _bindings[key].Add(new Binding<TMessage>(routingKey, handler: handler));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task Add<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var key = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _bindings.TryAdd(key, new HashSet<Binding>());
            _bindings[key].Add(new Binding<TMessage>(routingKey, handlerType: typeof(THandler)));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task Add<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>
    {
        var key = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _bindings.TryAdd(key, new HashSet<Binding>());
            _bindings[key].Add(new Binding<TMessage>(routingKey, handlerType: typeof(THandler)));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task Remove<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var key = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (_bindings.TryGetValue(key, out var set))
            {
                set.Remove(new Binding<TMessage>(routingKey, handler: handler));

                if (!set.Any()) _bindings.Remove(key);
            }
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
        var key = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (_bindings.TryGetValue(key, out var set))
            {
                set.Remove(new Binding<TMessage>(routingKey, handlerType: typeof(THandler)));

                if (!set.Any()) _bindings.Remove(key);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public IEnumerable<Binding<TMessage>> Get<TMessage>(string routingKey = "")
    {
        var key = new Route(typeof(TMessage), routingKey);
        
        return _bindings[key].Cast<Binding<TMessage>>();
    }

    private record Route(Type MessageType, string RoutingKey = "");
}