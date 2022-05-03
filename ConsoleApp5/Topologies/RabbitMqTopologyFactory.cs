using ConsoleApp5.Bindings;
using ConsoleApp5.Pipes;
using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp5.Topologies;

public class RabbitMqTopologyFactory : ITopologyFactory
{
    private readonly IPipeBinder _dispatchPipeBinder;
    private readonly IBus _bus;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public RabbitMqTopologyFactory(IPipeBinder dispatchPipeBinder, IBus bus, IServiceScopeFactory serviceScopeFactory)
    {
        _dispatchPipeBinder = dispatchPipeBinder;
        _bus = bus;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public Topology Create()
    {
        var handlerBinder = new HandlerBinder();
        var receivePipeBinder = new RabbitMqPipeBinder(_bus);
        var dispatchPipe = new RabbitMqPipe(_bus);
        var receivePipe = new HandlerPipe(handlerBinder, _serviceScopeFactory);
        return new Topology(dispatchPipe, receivePipe, _dispatchPipeBinder, receivePipeBinder, handlerBinder);
    }
}