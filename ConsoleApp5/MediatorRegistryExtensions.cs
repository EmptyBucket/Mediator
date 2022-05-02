using ConsoleApp5.Pipes;
using EasyNetQ;

namespace ConsoleApp5;

public static class MediatorRegistryExtensions
{
    public static void AddDefaultTransport(this IMediator mediator)
    {
        mediator.AddPipe("default", new HandlerPipe(mediator));
    }

    public static void AddRabbitMqTransport(this IMediator mediator, IBus bus)
    {
        mediator.AddPipe("rabbit", new RabbitMqPipe(bus));
    }
}