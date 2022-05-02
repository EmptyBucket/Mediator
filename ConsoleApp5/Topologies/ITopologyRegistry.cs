namespace ConsoleApp5.Topologies;

public interface ITopologyRegistry
{
    Task AddTopology<TMessage>(string routingKey = "");
    
    Task AddTopology<TMessage, TResult>(string routingKey = "");

    Task RemoveTopology<TMessage>(string routingKey = "");
}