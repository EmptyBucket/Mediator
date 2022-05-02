using ConsoleApp5.Pipes;

namespace ConsoleApp5.Registries;

public interface IPipeProvider
{
    (IPipe, IHandlerRegistry) GetTransport(string transportName);
}