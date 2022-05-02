using System.Collections.Concurrent;
using ConsoleApp5.Models;
using ConsoleApp5.Pipes;

namespace ConsoleApp5.Topologies;

internal class DirectTopologyRegistry : ITopologyRegistry, ITopologyProvider
{
    private readonly IPipe _pipe;
    private readonly ConcurrentDictionary<Route, Topology> _topologies = new();

    public DirectTopologyRegistry(IPipe pipe)
    {
        _pipe = pipe;
    }

    public Task AddTopology<TMessage>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _topologies[route] = new Topology(route, _pipe);

        return Task.CompletedTask;
    }

    public Task AddTopology<TMessage, TResult>(string routingKey = "")
    {
        return AddTopology<TMessage>(routingKey);
    }

    public Task RemoveTopology<TMessage>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _topologies.Remove(route, out _);

        return Task.CompletedTask;
    }

    public Topology? GetTopology<TMessage>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        return _topologies.TryGetValue(route, out var topology) ? topology : null;
    }
}