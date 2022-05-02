namespace ConsoleApp5.Topologies;

public interface ITopologyProvider
{
    IEnumerable<(string PipeName, IHandler Handler)> GetTopologies<TMessage>(string routingKey = "");
}