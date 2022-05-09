using Mediator.Configurations;
using Mediator.RabbitMq.Pipes;

namespace Mediator.RabbitMq.Configurations;

public static class PipeBinderExtensions
{
    public static IPipeBinder BindRabbitMq(this IPipeBinder pipeBinder, string pipeName = "rabbit") =>
        pipeBinder.BindPipe<RabbitMqPipe>(pipeName);
}