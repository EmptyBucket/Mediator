using Mediator.Configurations;
using Mediator.Pipes;
using Mediator.RabbitMq.Pipes;

namespace Mediator.RabbitMq.Configurations;

public static class PipeBindsBuilderExtensions
{
    public static IPipeBindsBuilder AddRabbitMq(this IPipeBindsBuilder pipeBindsBuilder, string pipeName = "rabbit") =>
        pipeBindsBuilder.BindPipe<RabbitMqPipe>(pipeName);
}