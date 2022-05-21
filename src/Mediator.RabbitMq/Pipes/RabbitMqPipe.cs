using EasyNetQ;
using Mediator.Pipes;

namespace Mediator.RabbitMq.Pipes;

internal class RabbitMqPipe : Pipe
{
    public RabbitMqPipe(IBus bus, IServiceProvider serviceProvider)
        : base(new RabbitMqPubPipe(bus, serviceProvider), new RabbitMqReqPipe(bus, serviceProvider))
    {
    }
}