using FlexMediator.Pipes;

namespace FlexMediator.Topologies;

public interface ITopologyFactory
{
    Topology Create(IPipeBinder dispatchPipeBinder);
}