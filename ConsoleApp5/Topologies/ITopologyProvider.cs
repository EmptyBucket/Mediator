using ConsoleApp5.Models;

namespace ConsoleApp5.Topologies;

public interface ITopologyProvider
{
    Topology? GetTopology<TMessage>(string routingKey = "");
}