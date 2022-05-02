using ConsoleApp5.Transports;

namespace ConsoleApp5.Topologies;

internal class TopologyRegistry : ITopologyRegistry, ITopologyProvider
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<(Type, string), HashSet<RegistryEntry>> _topologies = new();
    private readonly Dictionary<string, Transport> _transports;

    public TopologyRegistry(IEnumerable<Transport> transports)
    {
        _transports = transports.ToDictionary(t => t.Name);
    }

    public void AddTopology<TMessage>(IHandler handler, string transportName = "default", string routingKey = "")
    {
        var key = (typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _topologies.TryAdd(key, new HashSet<RegistryEntry>());
            _topologies[key].Add(new RegistryEntry(transportName, Handler: handler));
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void AddTopology<TMessage, THandler>(string transportName = "default", string routingKey = "")
        where THandler : IHandler
    {
        var key = (typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _topologies.TryAdd(key, new HashSet<RegistryEntry>());
            _topologies[key].Add(new RegistryEntry(transportName, HandlerType: typeof(THandler)));
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void RemoveTopology<TMessage>(IHandler handler, string transportName = "default", string routingKey = "")
    {
        var key = (typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (_topologies.TryGetValue(key, out var set))
            {
                set.Remove(new RegistryEntry(transportName, Handler: handler));

                if (!set.Any()) _topologies.Remove(key);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void RemoveTopology<TMessage, THandler>(string transportName = "default", string routingKey = "")
        where THandler : IHandler
    {
        var key = (typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (_topologies.TryGetValue(key, out var set))
            {
                set.Remove(new RegistryEntry(transportName, HandlerType: typeof(THandler)));

                if (!set.Any()) _topologies.Remove(key);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public IEnumerable<(string PipeName, IHandler Handler)> GetTopologies<TMessage>(string routingKey = "")
    {
        var key = (typeof(TMessage), routingKey);

        _lock.EnterReadLock();

        try
        {
            if (_topologies.TryGetValue(key, out var topologies))
                return topologies
                    .Select(t =>
                        (t.PipeName, t.Handler ?? (IHandler)_serviceProvider.GetRequiredService(t.HandlerType!)))
                    .ToArray();
        }
        finally
        {
            _lock.ExitReadLock();
        }

        return Array.Empty<(string PipeName, IHandler Handler)>();
    }

    private record RegistryEntry(string PipeName, IHandler? Handler = null, Type? HandlerType = null);
}