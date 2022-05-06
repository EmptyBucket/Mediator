using System.Collections;
using FlexMediator.Utils;

namespace FlexMediator.Pipes;

public class PipeConnector : IPipeConnector, IPipeConnections
{
    private readonly Dictionary<Route, HashSet<PipeConnection>> _pipeConnections = new();

    public Task<PipeConnection<TPipe>> Connect<TMessage, TPipe>(TPipe pipe, string routingKey = "")
        where TPipe : IPipe =>
        Connect(typeof(TMessage), pipe, routingKey: routingKey);

    public Task<PipeConnection<TPipe>> Connect<TMessage, TResult, TPipe>(TPipe pipe, string routingKey = "")
        where TPipe : IPipe =>
        Connect(typeof(TMessage), pipe, routingKey: routingKey, resultType: typeof(TResult));

    private Task<PipeConnection<TPipe>> Connect<TPipe>(Type messageType, TPipe pipe, string routingKey = "",
        Type? resultType = null)
        where TPipe : IPipe
    {
        var route = new Route(messageType, routingKey, resultType);
        var pipeBind = new PipeConnection<TPipe>(Disconnect, route, pipe);

        _pipeConnections.TryAdd(route, new HashSet<PipeConnection>());
        _pipeConnections[route].Add(pipeBind);

        return Task.FromResult(pipeBind);
    }

    private ValueTask Disconnect(PipeConnection pipeConnection)
    {
        if (_pipeConnections.TryGetValue(pipeConnection.Route, out var set))
        {
            set.Remove(pipeConnection);

            if (!set.Any()) _pipeConnections.Remove(pipeConnection.Route);
        }

        return ValueTask.CompletedTask;
    }

    #region IReadOnlyDictionary implementation

    public IEnumerator<KeyValuePair<Route, IReadOnlySet<PipeConnection>>> GetEnumerator()
    {
        return _pipeConnections.Cast<KeyValuePair<Route, IReadOnlySet<PipeConnection>>>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_pipeConnections).GetEnumerator();
    }

    public int Count => _pipeConnections.Count;

    public bool ContainsKey(Route key)
    {
        return _pipeConnections.ContainsKey(key);
    }

    public bool TryGetValue(Route key, out IReadOnlySet<PipeConnection> value)
    {
        var tryGetValue = _pipeConnections.TryGetValue(key, out var set);
        value = set!;
        return tryGetValue;
    }

    public IReadOnlySet<PipeConnection> this[Route key] => _pipeConnections[key];

    public IEnumerable<Route> Keys => _pipeConnections.Keys;

    public IEnumerable<IReadOnlySet<PipeConnection>> Values => _pipeConnections.Values;

    #endregion
}