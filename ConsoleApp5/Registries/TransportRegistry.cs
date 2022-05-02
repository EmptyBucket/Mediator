using System.Collections.Concurrent;
using ConsoleApp5.Pipes;

namespace ConsoleApp5.Registries;

public class TransportRegistry : ITransportRegistry, IPipeProvider
{
    private readonly ConcurrentDictionary<string, RegistryEntry> _pipes = new();

    public void AddPipe(string name, IPipe pipe, IHandlerRegistry handlerRegistry)
    {
        _pipes.TryAdd(name, new RegistryEntry(Pipe: pipe, HandlerRegistry: handlerRegistry));
    }

    public (IPipe, IHandlerRegistry) GetTransport(string transportName)
    {
        var (pipe, handlerRegistry) = _pipes[transportName];
        return (pipe, handlerRegistry);
    }

    private record RegistryEntry(IPipe Pipe, IHandlerRegistry HandlerRegistry);
}