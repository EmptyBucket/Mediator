using System.Text.Json.Serialization;
using Mediator.Pipes;

namespace Mediator.Handlers;

public record MessageContext<TMessage>(Route Route, string MessageId, string CorrelationId, DateTimeOffset CreatedAt)
{
    public DateTimeOffset? DeliveredAt { get; init; }

    public TimeSpan? DeliveryTime => DeliveredAt is not null ? DeliveredAt.Value - CreatedAt : null;

    public TMessage? Message { get; init; }

    public string? ExMessage { get; init; }

    [Newtonsoft.Json.JsonIgnore]
    [JsonIgnore]
    public IServiceProvider? ServiceProvider { get; init; }
}