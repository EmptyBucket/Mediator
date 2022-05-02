using ConsoleApp5.Bindings;
using ConsoleApp5.Pipes;

namespace ConsoleApp5.Transports;

public class Transport
{
    public Transport(string name, IPipe pipe, IBindingRegistry bindings)
    {
        Name = name;
        Pipe = pipe;
        Bindings = bindings;
    }

    public string Name { get; }

    public IPipe Pipe { get; }

    public IBindingRegistry Bindings { get; }
}