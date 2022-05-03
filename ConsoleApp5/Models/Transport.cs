using ConsoleApp5.Pipes;
using ConsoleApp5.TransportBindings;
using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp5.Models;

public readonly record struct Transport(string Name, IPipe Pipe, ITransportSubscriber Subscriber);

public class RabbitMqTransportFactory
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
        var rabbitMqPipe = new RabbitMqPipe(_bus);
        var directPipe = new HandlerForkPipe(_bus, _serviceScopeFactory);
        var rabbitMqTransportBinder = new RabbitMqTransportSubscriber(_bus);
        new Transport("rabbitmq", rabbitMqTransportBinder)
    }
}