using ConsoleApp5.Pipes;

namespace ConsoleApp5.Registries;

public interface ITransportRegistry
{
    void AddPipe(string name, IPipe pipe, IHandlerRegistry handlerRegistry);
}