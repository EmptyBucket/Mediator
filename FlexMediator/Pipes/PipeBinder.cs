using System.Collections;

namespace FlexMediator.Pipes;

internal class PipeBinder : IPipeBinder, IPipeBindings
{
    private readonly Dictionary<Route, HashSet<PipeBind>> _pipeBindings = new();

    public Task Bind<TMessage>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), RoutingKey: routingKey);

        _pipeBindings.TryAdd(route, new HashSet<PipeBind>());
        _pipeBindings[route].Add(new PipeBind(route, pipe));
        return Task.CompletedTask;
    }

    public Task Bind<TMessage, TResult>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), ResultType: typeof(TResult), RoutingKey: routingKey);

        _pipeBindings.TryAdd(route, new HashSet<PipeBind>());
        _pipeBindings[route].Add(new PipeBind(route, pipe));
        return Task.CompletedTask;
    }

    public Task Unbind<TMessage>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), RoutingKey: routingKey);

        if (_pipeBindings.TryGetValue(route, out var set))
        {
            set.Remove(new PipeBind(route, pipe));

            if (!set.Any()) _pipeBindings.Remove(route);
        }

        return Task.CompletedTask;
    }

    public Task Unbind<TMessage, TResult>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), ResultType: typeof(TResult), RoutingKey: routingKey);

        if (_pipeBindings.TryGetValue(route, out var set))
        {
            set.Remove(new PipeBind(route, pipe));

            if (!set.Any()) _pipeBindings.Remove(route);
        }

        return Task.CompletedTask;
    }

    public IEnumerable<PipeBind> GetBindings<TMessage>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), RoutingKey: routingKey);

        return _pipeBindings.TryGetValue(route, out var pipeBindings)
            ? pipeBindings
            : Enumerable.Empty<PipeBind>();
    }

    public IEnumerable<PipeBind> GetBindings<TMessage, TResult>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), ResultType: typeof(TResult), RoutingKey: routingKey);

        return _pipeBindings.TryGetValue(route, out var pipeBindings)
            ? pipeBindings
            : Enumerable.Empty<PipeBind>();
    }

    public IEnumerator<KeyValuePair<Route, IReadOnlySet<PipeBind>>> GetEnumerator()
    {
        return _pipeBindings.Cast<KeyValuePair<Route, IReadOnlySet<PipeBind>>>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_pipeBindings).GetEnumerator();
    }

    public int Count => _pipeBindings.Count;

    public bool ContainsKey(Route key)
    {
        return _pipeBindings.ContainsKey(key);
    }

    public bool TryGetValue(Route key, out IReadOnlySet<PipeBind> value)
    {
        var tryGetValue = _pipeBindings.TryGetValue(key, out var set);
        value = set!;
        return tryGetValue;
    }

    public IReadOnlySet<PipeBind> this[Route key] => _pipeBindings[key];

    public IEnumerable<Route> Keys => _pipeBindings.Keys;

    public IEnumerable<IReadOnlySet<PipeBind>> Values => _pipeBindings.Values;
}