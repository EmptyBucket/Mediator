namespace FlexMediator.Pipes.RedisMq;

internal readonly record struct RedisMqMessage<T>(string CorrelationId, T? Value = default, string? Exception = null);