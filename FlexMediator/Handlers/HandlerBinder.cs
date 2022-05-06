using System.Collections;

namespace FlexMediator.Handlers;

public class HandlerBinder : IHandlerBinder, IHandlerBindings
{
    private readonly Dictionary<Route, HashSet<HandlerBind>> _handlerBindings = new();

    public HandlerBind Bind<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage> =>
        Bind(typeof(TMessage), routingKey, handlerType: typeof(THandler));

    public HandlerBind Bind<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult> =>
        Bind(typeof(TMessage), routingKey, resultType: typeof(TResult), handlerType: typeof(THandler));

    public HandlerBind Bind<TMessage>(IHandler<TMessage> handler, string routingKey = "") =>
        Bind(typeof(TMessage), routingKey, handler: handler);

    public HandlerBind Bind<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "") =>
        Bind(typeof(TMessage), routingKey, resultType: typeof(TResult), handler: handler);

    private HandlerBind Bind(Type messageType, string routingKey = "", Type? resultType = null,
        Type? handlerType = null, IHandler? handler = null)
    {
        var route = new Route(messageType, routingKey, resultType);
        var handlerBind = new HandlerBind(Unbind, route, handlerType, handler);

        _handlerBindings.TryAdd(route, new HashSet<HandlerBind>());
        _handlerBindings[route].Add(handlerBind);

        return handlerBind;
    }

    private void Unbind(HandlerBind handlerBind)
    {
        if (_handlerBindings.TryGetValue(handlerBind.Route, out var set))
        {
            set.Remove(handlerBind);

            if (!set.Any()) _handlerBindings.Remove(handlerBind.Route);
        }
    }

    public IEnumerator<KeyValuePair<Route, IReadOnlySet<HandlerBind>>> GetEnumerator()
    {
        return _handlerBindings.Cast<KeyValuePair<Route, IReadOnlySet<HandlerBind>>>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_handlerBindings).GetEnumerator();
    }

    public int Count => _handlerBindings.Count;

    public bool ContainsKey(Route key)
    {
        return _handlerBindings.ContainsKey(key);
    }

    public bool TryGetValue(Route key, out IReadOnlySet<HandlerBind> value)
    {
        var tryGetValue = _handlerBindings.TryGetValue(key, out var set);
        value = set!;
        return tryGetValue;
    }

    public IReadOnlySet<HandlerBind> this[Route key] => _handlerBindings[key];

    public IEnumerable<Route> Keys => _handlerBindings.Keys;

    public IEnumerable<IReadOnlySet<HandlerBind>> Values => _handlerBindings.Values;
}