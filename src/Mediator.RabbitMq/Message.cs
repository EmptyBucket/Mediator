using EasyNetQ;

namespace Mediator.RabbitMq;

public class Message<T> : IMessage<T>
{
    public Message(T body) : this(body, MessageProperties.Empty)
    {
    }

    public Message(T body, in MessageProperties properties)
    {
        Body = body;
        Properties = properties;
        MessageType = typeof(T);
    }

    public MessageProperties Properties { get; }

    public Type MessageType { get; }

    public T Body { get; }

    public object? GetBody() => Body;
}