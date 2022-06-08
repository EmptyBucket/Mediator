using StackExchange.Redis;

namespace Mediator.Redis.Utils;

internal class MessageProperties
{
    public RedisValue? MessageId { get; set; } = null;

    public int? MaxLenght { get; set; } = null;

    public bool UseApproximateMaxLength { get; set; } = false;

    public CommandFlags Flags { get; set; } = CommandFlags.None;
}