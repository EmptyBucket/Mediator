using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Mediator.Pipes.Utils;

namespace Mediator.Handlers;

public record MessageContext<TMessage>
{
    private IImmutableDictionary<string, object?> _meta;
    private IImmutableDictionary<string, object?> _extra;

    [JsonConstructor]
    [Newtonsoft.Json.JsonConstructor]
    public MessageContext(Route route, TMessage message,
        IImmutableDictionary<string, object?> meta, IImmutableDictionary<string, object?> extra)
    {
        Route = route;
        Message = message;
        _meta = meta;
        _extra = extra;
        CreatedAt = DateTimeOffset.Now;
    }

    public MessageContext(MessageContext<TMessage> ctx)
    {
        Route = ctx.Route;
        Message = ctx.Message;
        _meta = ctx._meta;
        _extra = ctx._extra;
    }

    public Route Route { get; init; }

    public TMessage Message { get; init; }

    public string? MessageId
    {
        get => Get<string>(nameof(MessageId));
        init => Set(nameof(MessageId), value);
    }

    public string? CorrelationId
    {
        get => Get<string>(nameof(CorrelationId));
        init => Set(nameof(CorrelationId), value);
    }

    public DateTimeOffset CreatedAt
    {
        get => Get<DateTimeOffset>(nameof(CreatedAt), true);
        init => Set(nameof(CreatedAt), value, true);
    }

    public DateTimeOffset? DeliveredAt
    {
        get => Get<DateTimeOffset>(nameof(DeliveredAt), true);
        init => Set(nameof(DeliveredAt), value, true);
    }

    public TimeSpan? DeliveryTime => DeliveredAt is not null ? DeliveredAt.Value - CreatedAt : null;

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public IServiceProvider? ServiceProvider { get; init; }

    public T? Get<T>(string propertyName, bool isExtra = false) =>
        isExtra ? (T?)Extra.GetValueOrDefault(propertyName) : (T?)Meta.GetValueOrDefault(propertyName);

    private void Set<T>(string propertyName, T value, bool isExtra = false)
    {
        if (isExtra) _extra = _extra.SetItem(propertyName, value);
        else _meta = _meta.SetItem(propertyName, value);
    }

    public IImmutableDictionary<string, object?> Meta
    {
        get => _meta;
        init => _meta = value;
    }

    public IImmutableDictionary<string, object?> Extra
    {
        get => _extra;
        init => _extra = value;
    }
}