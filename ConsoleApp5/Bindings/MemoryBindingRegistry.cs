namespace ConsoleApp5.Bindings;

internal class MemoryBindingRegistry : IBindingRegistry, IBindingProvider
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<Route, HashSet<Binding>> _bindings = new();

    public Task Add<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _bindings.TryAdd(route, new HashSet<Binding>());
            _bindings[route].Add(new Binding(route, handler: handler));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task Add<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _bindings.TryAdd(route, new HashSet<Binding>());
            _bindings[route].Add(new Binding(route, handler: handler));
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
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _bindings.TryAdd(route, new HashSet<Binding>());
            _bindings[route].Add(new Binding(route, handlerType: typeof(THandler)));
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
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _bindings.TryAdd(route, new HashSet<Binding>());
            _bindings[route].Add(new Binding(route, handlerType: typeof(THandler)));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task Remove<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (_bindings.TryGetValue(route, out var set))
            {
                set.Remove(new Binding(route, handler: handler));

                if (!set.Any()) _bindings.Remove(route);
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
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (_bindings.TryGetValue(route, out var set))
            {
                set.Remove(new Binding(route, handlerType: typeof(THandler)));

                if (!set.Any()) _bindings.Remove(route);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public IEnumerable<Binding> Get<TMessage>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        return _bindings.TryGetValue(route, out var bindings) ? bindings : Enumerable.Empty<Binding>();
    }
}