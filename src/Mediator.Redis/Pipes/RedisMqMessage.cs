namespace Mediator.Redis.Pipes;

internal readonly record struct RedisMqMessage<T>(string CorrelationId, T? Value = default, string? Exception = null);