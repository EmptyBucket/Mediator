using System.Text.Json.Serialization;
using Mediator.Pipes.Utils;

namespace Mediator.Handlers;

public record MessageContext<TMessage>(Route Route, string MessageId, string CorrelationId, DateTimeOffset CreatedAt,
    TMessage Message)
{
    public Route Route { get; set; } = Route;

    public string MessageId { get; set; } = MessageId;

    public string CorrelationId { get; set; } = CorrelationId;

    public DateTimeOffset? DeliveredAt { get; init; }

    public TimeSpan? DeliveryTime => DeliveredAt is not null ? DeliveredAt.Value - CreatedAt : null;

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public IServiceProvider? ServiceProvider { get; init; }
}