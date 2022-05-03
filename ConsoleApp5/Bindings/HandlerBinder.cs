namespace ConsoleApp5.Bindings;

internal class HandlerBinder : IHandlerBinder, IHandlerBindProvider
{
    private readonly Dictionary<Route, HashSet<HandlerBind>> _handlerBindings = new();

    public void Bind<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), RoutingKey: routingKey);

        _handlerBindings.TryAdd(route, new HashSet<HandlerBind>());
        _handlerBindings[route].Add(new HandlerBind(route, handler: handler));
    }

    public void Bind<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), ResultType: typeof(TResult), RoutingKey: routingKey);

        _handlerBindings.TryAdd(route, new HashSet<HandlerBind>());
        _handlerBindings[route].Add(new HandlerBind(route, handler: handler));
    }

    public void Bind<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var route = new Route(typeof(TMessage), RoutingKey: routingKey);

        _handlerBindings.TryAdd(route, new HashSet<HandlerBind>());
        _handlerBindings[route].Add(new HandlerBind(route, handlerType: typeof(THandler)));
    }

    public void Bind<TMessage, TResult, THandler>(string routingKey = "") 
        where THandler : IHandler<TMessage, TResult>
    {
        var route = new Route(typeof(TMessage), ResultType: typeof(TResult), RoutingKey: routingKey);

        _handlerBindings.TryAdd(route, new HashSet<HandlerBind>());
        _handlerBindings[route].Add(new HandlerBind(route, handlerType: typeof(THandler)));
    }

    public void Unbind<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), RoutingKey: routingKey);

        if (_handlerBindings.TryGetValue(route, out var set))
        {
            set.Remove(new HandlerBind(route, handler: handler));

            if (!set.Any()) _handlerBindings.Remove(route);
        }
    }

    public void Unbind<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), ResultType: typeof(TResult), RoutingKey: routingKey);

        if (_handlerBindings.TryGetValue(route, out var set))
        {
            set.Remove(new HandlerBind(route, handler: handler));

            if (!set.Any()) _handlerBindings.Remove(route);
        }
    }

    public void Unbind<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var route = new Route(typeof(TMessage), RoutingKey: routingKey);

        if (_handlerBindings.TryGetValue(route, out var set))
        {
            set.Remove(new HandlerBind(route, handlerType: typeof(THandler)));

            if (!set.Any()) _handlerBindings.Remove(route);
        }
    }

    public void Unbind<TMessage, TResult, THandler>(string routingKey = "") 
        where THandler : IHandler<TMessage, TResult>
    {
        var route = new Route(typeof(TMessage), ResultType: typeof(TResult), RoutingKey: routingKey);

        if (_handlerBindings.TryGetValue(route, out var set))
        {
            set.Remove(new HandlerBind(route, handlerType: typeof(THandler)));

            if (!set.Any()) _handlerBindings.Remove(route);
        }
    }

    public IEnumerable<HandlerBind> GetBindings<TMessage>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), RoutingKey: routingKey);

        return _handlerBindings.TryGetValue(route, out var bindings) ? bindings : Enumerable.Empty<HandlerBind>();
    }
    
    public IEnumerable<HandlerBind> GetBindings<TMessage, TResult>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), ResultType: typeof(TResult), RoutingKey: routingKey);

        return _handlerBindings.TryGetValue(route, out var bindings) ? bindings : Enumerable.Empty<HandlerBind>();
    }
}