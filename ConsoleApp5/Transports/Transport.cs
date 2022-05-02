using ConsoleApp5.Bindings;
using ConsoleApp5.Pipes;
using ConsoleApp5.Topologies;

namespace ConsoleApp5.Transports;

public class Transport
{
    public Transport(string name, IPipe pipe, IBindingRegistry bindings)
    {
        Name = name;
    }

    public string Name { get; }

    public ITopologyRegistry TopologyRegistry { get; }
}