using System.Collections;

namespace FlexMediator.Pipes;

internal class PipeBinder : IPipeBinder, IPipeBindings
{
    private readonly Dictionary<Route, HashSet<PipeBind>> _pipeBindings = new();

    public Task<PipeBind> Bind<TMessage>(IPipe pipe, string routingKey = "") =>
        Bind(typeof(TMessage), pipe, routingKey: routingKey);

    public Task<PipeBind> Bind<TMessage, TResult>(IPipe pipe, string routingKey = "") =>
        Bind(typeof(TMessage), pipe, routingKey: routingKey, resultType: typeof(TResult));

    private Task<PipeBind> Bind(Type messageType, IPipe pipe, string routingKey = "", Type? resultType = null)
    {
        var route = new Route(messageType, routingKey, resultType);
        var pipeBind = new PipeBind(Unbind, route, pipe);

        _pipeBindings.TryAdd(route, new HashSet<PipeBind>());
        _pipeBindings[route].Add(pipeBind);

        return Task.FromResult(pipeBind);
    }

    private ValueTask Unbind(PipeBind pipeBind)
    {
        if (_pipeBindings.TryGetValue(pipeBind.Route, out var set))
        {
            set.Remove(pipeBind);

            if (!set.Any()) _pipeBindings.Remove(pipeBind.Route);
        }

        return ValueTask.CompletedTask;
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