using ConsoleApp5.Pipes;

namespace ConsoleApp5.Bindings;

internal class PipeBinder : IPipeBinder, IPipeBindProvider
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
}