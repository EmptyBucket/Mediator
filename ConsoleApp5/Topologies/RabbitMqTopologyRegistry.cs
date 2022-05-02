using ConsoleApp5.Models;
using ConsoleApp5.Pipes;
using EasyNetQ;

namespace ConsoleApp5.Topologies;

internal class RabbitMqTopologyRegistry : ITopologyRegistry, ITopologyProvider
{
    private readonly IBus _bus;
    private readonly IPipe _dispatchPipe;
    private readonly IPipe _receivePipe;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<Route, (Topology Topology, IDisposable Subscription)> _topologies = new();

    public RabbitMqTopologyRegistry(IBus bus, IPipe dispatchPipe, IPipe receivePipe)
    {
        _bus = bus;
        _dispatchPipe = dispatchPipe;
        _receivePipe = receivePipe;
    }

    public async Task AddTopology<TMessage>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (!_topologies.ContainsKey(route))
            {
                var subscription = await _bus.PubSub.SubscribeAsync<TMessage>(routingKey,
                    (m, c) => _receivePipe.Handle(m, new MessageOptions(routingKey), c),
                    c => c.WithDurable(false).WithAutoDelete());
                _topologies[route] = (new Topology(route, _dispatchPipe), subscription);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public async Task AddTopology<TMessage, TResult>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (!_topologies.ContainsKey(route))
            {
                var subscription = await _bus.Rpc.RespondAsync<TMessage, TResult>(
                    (m, c) => _receivePipe.Handle<TMessage, TResult>(m, new MessageOptions(routingKey), c),
                    c => c.WithDurable(false));
                _topologies[route] = (new Topology(route, _dispatchPipe), subscription);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task RemoveTopology<TMessage>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (_topologies.Remove(route, out var topology)) topology.Subscription.Dispose();
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Topology? GetTopology<TMessage>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        return _topologies.TryGetValue(route, out var topology) ? topology.Topology : null;
    }
}