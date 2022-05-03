using EasyNetQ;
using FlexMediator.Handlers;
using FlexMediator.Pipes;
using FlexMediator.Topologies;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediatorRabbit;

public class RabbitMqTopologyFactory : ITopologyFactory
{
    private readonly IBus _bus;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public RabbitMqTopologyFactory(IBus bus, IServiceScopeFactory serviceScopeFactory)
    {
        _bus = bus;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public Topology Create(IPipeBinder dispatchPipeBinder)
    {
        var handlerBinder = new HandlerBinder();
        var receivePipeBinder = new RabbitMqPipeBinder(_bus);
        var dispatchPipe = new RabbitMqPipe(_bus);
        var receivePipe = new HandlerPipe(handlerBinder, _serviceScopeFactory);
        return new Topology(dispatchPipe, receivePipe, _dispatchPipeBinder, receivePipeBinder, handlerBinder);
    }
}