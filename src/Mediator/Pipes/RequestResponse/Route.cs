namespace Mediator.RequestResponse;

public readonly record struct Route(Type MessageType, Type ResultType, string RoutingKey = "")
{
    public static Route For<TMessage, TResult>(string routingKey = "") =>
        new(typeof(TMessage), typeof(TResult), routingKey);

    public static implicit operator string(Route route) => route.ToString();
}