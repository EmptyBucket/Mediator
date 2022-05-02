using ConsoleApp5.Bindings;
using ConsoleApp5.Pipes;

namespace ConsoleApp5.Topologies;

internal class TopologyRegistry : ITopologyRegistry, ITopologyProvider
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<Route, HashSet<Topology>> _topologies = new();

    public Task AddTopology<TMessage>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _topologies.TryAdd(route, new HashSet<Topology>());
            _topologies[route].Add(new Topology(route, pipe));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task RemoveTopology<TMessage>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (_topologies.TryGetValue(route, out var set))
            {
                set.Remove(new Topology(route, pipe));

                if (!set.Any()) _topologies.Remove(route);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public IEnumerable<Topology> GetTopology<TMessage>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        return _topologies.TryGetValue(route, out var topologies) ? topologies : Enumerable.Empty<Topology>();
    }
}