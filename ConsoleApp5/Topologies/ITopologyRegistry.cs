using ConsoleApp5.Pipes;

namespace ConsoleApp5.Topologies;

public interface ITopologyRegistry
{
    Task AddTopology<TMessage>(IPipe pipe, string routingKey = "");

    Task RemoveTopology<TMessage>(IPipe pipe, string routingKey = "");
}

public interface ITopologyProvider
{
    IEnumerable<Topology> GetTopology<TMessage>(string routingKey = "");
}