using Mediator.Configurations;
using Mediator.Redis.Pipes;

namespace Mediator.Redis.Configurations;

public static class PipeBinderExtensions
{
    public static IPipeBinder BindRedisMq(this IPipeBinder pipeBinder) =>
        pipeBinder
            .Bind<RedisMqPipe>()
            .BindInterfaces<RedisMqPipe>(nameof(RedisMqPipe));
}