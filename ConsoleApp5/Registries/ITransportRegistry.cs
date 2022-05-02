using ConsoleApp5.Pipes;

namespace ConsoleApp5.Registries;

public interface ITransportRegistry
{
    void AddTransport<TPipe>(string name) where TPipe : IPipe;
    
    void RemoveTransport(string name);
}