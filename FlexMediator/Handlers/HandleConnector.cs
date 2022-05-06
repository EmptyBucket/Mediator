using System.Collections;
using FlexMediator.Utils;

namespace FlexMediator.Handlers;

public class HandleConnector : IHandleConnector, IHandlerConnections
{
    private readonly Dictionary<Route, HashSet<HandlerConnection>> _handlerConnections = new();

    public HandlerConnection Connect<TMessage>(Func<IServiceProvider, IHandler> factory,
        string routingKey = "") =>
        Connect(typeof(TMessage), factory, routingKey);

    public HandlerConnection Connect<TMessage, TResult>(Func<IServiceProvider, IHandler> factory,
        string routingKey = "") =>
        Connect(typeof(TMessage), factory, routingKey, typeof(TResult));

    private HandlerConnection Connect(Type messageType, Func<IServiceProvider, IHandler> factory,
        string routingKey = "", Type? resultType = null)
    {
        var route = new Route(messageType, routingKey, resultType);
        var handlerBind = new HandlerConnection(Disconnect, route, factory);

        _handlerConnections.TryAdd(route, new HashSet<HandlerConnection>());
        _handlerConnections[route].Add(handlerBind);

        return handlerBind;
    }

    private void Disconnect(HandlerConnection handlerConnection)
    {
        if (_handlerConnections.TryGetValue(handlerConnection.Route, out var set))
        {
            set.Remove(handlerConnection);

            if (!set.Any()) _handlerConnections.Remove(handlerConnection.Route);
        }
    }

    #region IReadOnlyDictionary implementation

    public IEnumerator<KeyValuePair<Route, IReadOnlySet<HandlerConnection>>> GetEnumerator()
    {
        return _handlerConnections.Cast<KeyValuePair<Route, IReadOnlySet<HandlerConnection>>>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_handlerConnections).GetEnumerator();
    }

    public int Count => _handlerConnections.Count;

    public bool ContainsKey(Route key)
    {
        return _handlerConnections.ContainsKey(key);
    }

    public bool TryGetValue(Route key, out IReadOnlySet<HandlerConnection> value)
    {
        var tryGetValue = _handlerConnections.TryGetValue(key, out var set);
        value = set!;
        return tryGetValue;
    }

    public IReadOnlySet<HandlerConnection> this[Route key] => _handlerConnections[key];

    public IEnumerable<Route> Keys => _handlerConnections.Keys;

    public IEnumerable<IReadOnlySet<HandlerConnection>> Values => _handlerConnections.Values;

    #endregion
}