namespace Mediator.Pipes;

public readonly record struct Route(Type MessageType, Type? ResultType = null, string RoutingKey = "")
{
    public static Route For<TMessage>(string routingKey = "") =>
        new(typeof(TMessage), RoutingKey: routingKey);

    public static Route For<TMessage, TResult>(string routingKey = "") =>
        new(typeof(TMessage), typeof(TResult), routingKey);

    public static implicit operator string(Route route) => route.ToString();
}