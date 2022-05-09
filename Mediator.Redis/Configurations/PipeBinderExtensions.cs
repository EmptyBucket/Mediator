using Mediator.Configurations;
using Mediator.Redis.Pipes;

namespace Mediator.Redis.Configurations;

public static class PipeBinderExtensions
{
    public static IPipeBinder BindRedisMq(this IPipeBinder pipeBinder, string pipeName = "redis") =>
        pipeBinder.BindPipe<RedisMqPipe>(pipeName);
}