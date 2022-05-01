using ConsoleApp5.Pipes;

namespace ConsoleApp5.Registries;

public interface IPipeRegistry
{
    void AddPipe(IPipe pipe, string? routingKey = null);
    
    void AddPipe<TMessage>(IPipe pipe, string? routingKey = null);

    void RemovePipe(IPipe pipe, string? routingKey = null);
    
    void RemovePipe<TMessage>(IPipe pipe, string? routingKey = null);
}