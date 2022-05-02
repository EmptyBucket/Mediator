namespace ConsoleApp5.Bindings;

public record Binding
{
    public Binding(Type messageType, string routingKey, IHandler? handler = null, Type? handlerType = null)
    {
        if (handler is null && handlerType is null)
            throw new ArgumentException($"{nameof(handler)} or {nameof(handlerType)} must be not-null");

        MessageType = messageType;
        RoutingKey = routingKey;
        Handler = handler;
        HandlerType = handlerType;
    }

    public Type MessageType { get; }

    public string RoutingKey { get; }

    public IHandler? Handler { get; }

    public Type? HandlerType { get; }
}

public record Binding<TMessage> : Binding
{
    public Binding(string routingKey, IHandler<TMessage>? handler = null, Type? handlerType = null)
        : base(typeof(TMessage), routingKey, handler, handlerType)
    {
        Handler = handler;
    }

    public new IHandler<TMessage>? Handler { get; }
}