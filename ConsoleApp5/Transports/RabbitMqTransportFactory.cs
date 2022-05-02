using ConsoleApp5.Bindings;
using ConsoleApp5.Pipes;
using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp5.Transports;

public class RabbitMqTransportFactory : ITransportFactory
{
    private readonly IBus _bus;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public RabbitMqTransportFactory(IBus bus, IServiceScopeFactory serviceScopeFactory)
    {
        _bus = bus;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public Transport Create()
    {
        var pipe = new RabbitMqPipe(_bus);
        var bindings = new RabbitMqBindingRegistry(_bus, _serviceScopeFactory);
        return new Transport("rabbitmq", pipe, bindings);
    }
}