using EasyNetQ;
using FlexMediator.Handlers;
using FlexMediator.Pipes;
using FlexMediator.Topologies;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediatorRabbit;

public class RabbitMqTopologyFactory : ITopologyFactory
{
    private readonly IPipeBinder _dispatchPipeBuilder;
    private readonly IBus _bus;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public RabbitMqTopologyFactory(IPipeBinder dispatchPipeBuilder, IBus bus, IServiceScopeFactory serviceScopeFactory)
    {
        _dispatchPipeBuilder = dispatchPipeBuilder;
        _bus = bus;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public Topology Create()
    {
        var handlerBinder = new HandlerBinder();
        var receivePipeBinder = new RabbitMqPipeBinder(_bus);
        var dispatchPipe = new RabbitMqPipe(_bus);
        var receivePipe = new HandlingPipe(handlerBinder, _serviceScopeFactory);
        return new Topology(dispatchPipe, receivePipe, _dispatchPipeBuilder, receivePipeBinder, handlerBinder);
    }
}