using Mediator.Pipes;
using StackExchange.Redis;

namespace Mediator.Redis.Pipes;

public class RedisMqPipe : Pipe
{
    public RedisMqPipe(IConnectionMultiplexer multiplexer, IServiceProvider serviceProvider)
        : base(new RedisMqPubPipe(multiplexer, serviceProvider), new RedisMqReqPipe(multiplexer, serviceProvider))
    {
    }
}