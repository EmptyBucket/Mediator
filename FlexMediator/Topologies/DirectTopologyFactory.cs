using FlexMediator.Handlers;
using FlexMediator.Pipes;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediator.Topologies;

public class DirectTopologyFactory : ITopologyFactory
{
    private readonly IPipeBinder _dispatchPipeBinder;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public DirectTopologyFactory(IPipeBinder dispatchPipeBinder, IServiceScopeFactory serviceScopeFactory)
    {
        _dispatchPipeBinder = dispatchPipeBinder;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public TopologyBinder Create()
    {
        var handlerBinder = new HandlerBinder();
        var dispatchReceivePipe = new HandlingPipe(handlerBinder, _serviceScopeFactory);
        return new TopologyBinder(dispatchReceivePipe, dispatchReceivePipe, _dispatchPipeBinder, _dispatchPipeBinder,
            handlerBinder);
    }
}