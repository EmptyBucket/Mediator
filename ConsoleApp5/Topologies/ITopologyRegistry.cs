namespace ConsoleApp5.Topologies;

public interface ITopologyRegistry
{
    void AddTopology<TMessage>(IHandler handler, string transportName = "default", string routingKey = "");

    void AddTopology<TMessage, THandler>(string transportName = "default", string routingKey = "")
        where THandler : IHandler;

    void RemoveTopology<TMessage>(IHandler handler, string transportName = "default", string routingKey = "");

    void RemoveTopology<TMessage, THandler>(string transportName = "default", string routingKey = "")
        where THandler : IHandler;
}