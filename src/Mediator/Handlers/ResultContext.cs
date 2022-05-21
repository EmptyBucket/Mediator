using Mediator.Pipes.Utils;

namespace Mediator.Handlers;

public record ResultContext<TResult>(Route Route, string MessageId, string CorrelationId, DateTimeOffset CreatedAt)
{
    public DateTimeOffset? DeliveredAt { get; init; }

    public TimeSpan? DeliveryTime => DeliveredAt is not null ? DeliveredAt.Value - CreatedAt : null;

    public TResult? Result { get; set; }

    public Exception? Exception { get; set; }
};