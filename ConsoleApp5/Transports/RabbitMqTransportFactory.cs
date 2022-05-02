using ConsoleApp5.Pipes;
using ConsoleApp5.Topologies;
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
        var bindings = new RabbitMqTopologyRegistry(_bus, receiveTransport);
        return new Transport("rabbitmq", pipe, bindings);
    }
}