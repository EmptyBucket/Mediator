using System.Reflection;
using EasyNetQ;

namespace Mediator.RabbitMq.Utils;

internal class MessagePropertiesBuilder
{
    private static readonly Dictionary<string, PropertyInfo> Props = typeof(MessageProperties)
        .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty)
        .ToDictionary(p => p.Name);

    private readonly MessageProperties _messageProperties;

    public MessagePropertiesBuilder()
    {
        _messageProperties = new MessageProperties();
    }

    public MessagePropertiesBuilder Attach(IEnumerable<KeyValuePair<string, object?>> values)
    {
        foreach (var value in values)
            if (Props.TryGetValue(value.Key, out var prop))
                prop.SetValue(_messageProperties, value.Value);

        return this;
    }

    public MessageProperties Build() => _messageProperties;
}