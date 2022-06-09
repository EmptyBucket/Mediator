using Mediator.Configurations;
using Mediator.Redis.Pipes;

namespace Mediator.Redis.Configurations;

public static class PipeBinderExtensions
{
    public static IPipeBinder BindRedis(this IPipeBinder pipeBinder) =>
        pipeBinder
            .Bind<RedisMqPipe>()
            .BindInterfaces<RedisMqPipe>(nameof(RedisMqPipe))
            .Bind<RedisStreamPubPipe>()
            .BindInterfaces<RedisStreamPubPipe>(nameof(RedisStreamPubPipe));
}