using ConsoleApp5.Pipes;

namespace ConsoleApp5.Bindings;

internal class PipeBinder : IPipeBinder, IPipeBindProvider
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<Route, HashSet<PipeBind>> _pipeBindings = new();

    public Task Bind<TMessage>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _pipeBindings.TryAdd(route, new HashSet<PipeBind>());
            _pipeBindings[route].Add(new PipeBind(route, pipe));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task Bind<TMessage, TResult>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _pipeBindings.TryAdd(route, new HashSet<PipeBind>());
            _pipeBindings[route].Add(new PipeBind(route, pipe));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task Unbind<TMessage>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (_pipeBindings.TryGetValue(route, out var set))
            {
                set.Remove(new PipeBind(route, pipe));

                if (!set.Any()) _pipeBindings.Remove(route);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public IEnumerable<PipeBind> GetBindings<TMessage>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        return _pipeBindings.TryGetValue(route, out var pipeBindings)
            ? pipeBindings
            : Enumerable.Empty<PipeBind>();
    }
}