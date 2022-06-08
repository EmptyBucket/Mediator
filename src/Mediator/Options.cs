namespace Mediator;

public class Options
{
    public string? MessageId
    {
        get => Get<string>(nameof(MessageId));
        set => Set(nameof(MessageId), value);
    }

    public string? CorrelationId
    {
        get => Get<string>(nameof(CorrelationId));
        set => Set(nameof(CorrelationId), value);
    }

    public string RoutingKey
    {
        get => Get<string>(nameof(RoutingKey), true) ?? string.Empty;
        set => Set(nameof(RoutingKey), value, true);
    }

    public T? Get<T>(string propertyName, bool isExtra = false) =>
        isExtra ? (T?)Extra.GetValueOrDefault(propertyName) : (T?)Meta.GetValueOrDefault(propertyName);

    public void Set<T>(string propertyName, T value, bool isExtra = false)
    {
        if (isExtra) Extra[propertyName] = value;
        else Meta[propertyName] = value;
    }

    /// <summary>
    /// Service information is embedded in the fields of the same name 
    /// </summary>
    public Dictionary<string, object?> Meta { get; set; } = new();

    /// <summary>
    /// Additional information stored in the body 
    /// </summary>
    public Dictionary<string, object?> Extra { get; set; } = new();
}