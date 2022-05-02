using ConsoleApp5.Pipes;
using ConsoleApp5.Registries;
using Microsoft.Extensions.Logging;

namespace ConsoleApp5;

internal class MediatorBuilder
{
    private IPipe _pipe;

    public MediatorBuilder(TopologyRegistry topologyRegistry, ITransportRegistry transportRegistry)
    {
        Topology = topologyRegistry;
        Transport = transportRegistry;
        _pipe = new ForkingPipe(topologyRegistry);
    }

    public ITopologyRegistry Topology { get; }

    public ITransportRegistry Transport { get; }

    public IMediator Build()
    {
        var mediator = new Mediator(_pipe, Topology);
        return mediator;
    }
}