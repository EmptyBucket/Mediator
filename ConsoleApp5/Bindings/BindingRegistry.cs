using ConsoleApp5.Models;

namespace ConsoleApp5.Bindings;

internal class BindingRegistry : IBindingRegistry, IBindingProvider
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<Route, HashSet<Binding>> _bindings = new();

    public Task AddBinding<TMessage>(IHandler<TMessage> handler, string routingKey = "")
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

    public Task AddBinding<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "")
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

    public Task AddBinding<TMessage, THandler>(string routingKey = "")
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

    public Task AddBinding<TMessage, TResult, THandler>(string routingKey = "")
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

    public Task RemoveBinding<TMessage>(IHandler<TMessage> handler, string routingKey = "")
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

    public Task RemoveBinding<TMessage, THandler>(string routingKey = "")
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

    public IEnumerable<Binding> GetBindings<TMessage>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        return _bindings.TryGetValue(route, out var bindings) ? bindings : Enumerable.Empty<Binding>();
    }
}