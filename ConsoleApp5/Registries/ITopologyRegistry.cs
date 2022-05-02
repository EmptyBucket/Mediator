namespace ConsoleApp5.Registries;

public interface ITopologyRegistry
{
    void AddTopology<TMessage>(IHandler handler, string pipeName = "default", string routingKey = "");

    void AddTopology<TMessage, THandler>(string pipeName = "default", string routingKey = "")
        where THandler : IHandler;

    void RemoveTopology<TMessage>(IHandler handler, string pipeName = "default", string routingKey = "");

    void RemoveTopology<TMessage, THandler>(string pipeName = "default", string routingKey = "")
        where THandler : IHandler;
}