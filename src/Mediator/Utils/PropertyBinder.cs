using System.Reflection;

namespace Mediator.Utils;

internal class PropertyBinder<TMessageProperties> where TMessageProperties : new()
{
    private static readonly Dictionary<string, PropertyInfo> Props = typeof(TMessageProperties)
        .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty)
        .ToDictionary(p => p.Name);

    private readonly TMessageProperties _messageProperties;

    public PropertyBinder()
    {
        _messageProperties = new TMessageProperties();
    }

    public PropertyBinder<TMessageProperties> Bind(IEnumerable<KeyValuePair<string, object?>> pairs)
    {
        foreach (var pair in pairs)
            if (Props.TryGetValue(pair.Key, out var prop))
                prop.SetValue(_messageProperties, pair.Value?.CastTo(prop.PropertyType));

        return this;
    }

    public TMessageProperties Build() => _messageProperties;
}