using Mediator.Configurations;
using Mediator.Pipes;
using Mediator.Redis.Pipes;

namespace Mediator.Redis.Configurations;

public static class PipeBindsBuilderExtensions
{
    public static IPipeBindsBuilder AddRedisMq(this IPipeBindsBuilder pipeBindsBuilder, string pipeName = "redis") =>
        pipeBindsBuilder.BindPipe<RedisMqPipe>(pipeName);
}