using ConsoleApp5.Bindings;
using ConsoleApp5.Pipes;
using EasyNetQ;

namespace ConsoleApp5.Transports;

public class RabbitMqTransportFactory : ITransportFactory
{
    private readonly IBus _bus;
    private readonly ITransportFactory _receiveTransportFactory;

    public RabbitMqTransportFactory(IBus bus, ITransportFactory receiveTransportFactory)
    {
        _bus = bus;
        _receiveTransportFactory = receiveTransportFactory;
    }

    public Transport Create()
    {
        var pipe = new RabbitMqPipe(_bus);
        var receiveTransport = _receiveTransportFactory.Create();
        var bindings = new RabbitMqBindingRegistry(_bus, receiveTransport);
        return new Transport("rabbitmq", pipe, bindings);
    }
}