using ConsoleApp5.Pipes;

namespace ConsoleApp5.Registries;

public interface ITopologyRegistry
{
    void AddTopology<TMessage>(IHandler handler, IPipe pipe, string routingKey = "");
    
    void AddTopology<TMessage>(IHandler handler, string transportName = "default", string routingKey = "");

    void AddTopology<TMessage, THandler>(IPipe pipe, string routingKey = "")
        where THandler : IHandler;

    void AddTopology<TMessage, THandler>(string transportName = "default", string routingKey = "")
        where THandler : IHandler;

    void RemoveTopology<TMessage>(IHandler handler, string routingKey = "");

    void RemoveTopology<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler;
}