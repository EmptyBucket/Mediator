using ConsoleApp5.Pipes;

namespace ConsoleApp5.Registries;

public interface ITransportProvider
{
    IPipe GetTransport(string name);
}