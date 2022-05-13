using Mediator.Configurations;
using Mediator.RabbitMq.Pipes;

namespace Mediator.RabbitMq.Configurations;

public static class PipeBinderExtensions
{
    public static IPipeBinder BindRabbitMq(this IPipeBinder pipeBinder) =>
        pipeBinder
            .Bind<RabbitMqPipe>()
            .BindInterfaces<RabbitMqPipe>(nameof(RabbitMqPipe));
}