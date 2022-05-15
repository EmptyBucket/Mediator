namespace Mediator.Pipes;

public readonly record struct Route(string MessageType, string RoutingKey = "", string? ResultType = null)
{
    public Route(Type messageType, string routingKey = "", Type? resultType = null)
        : this(messageType.FullName ?? messageType.Name, routingKey, resultType?.FullName ?? resultType?.Name)
    {
    }

    public static Route For<TMessage>(string routingKey = "") =>
        new(typeof(TMessage), routingKey);

    public static Route For<TMessage, TResult>(string routingKey = "") =>
        new(typeof(TMessage), routingKey, typeof(TResult));

    public static implicit operator string(Route route) => route.ToString();

    public override string ToString() =>
        $"{MessageType}:{RoutingKey}{(ResultType is not null ? $":{ResultType}" : string.Empty)}";
}