namespace Mediator.PubSub;

public readonly record struct Route(Type MessageType, string RoutingKey = "")
{
    public static Route For<TMessage>(string routingKey = "") => new(typeof(TMessage), routingKey);

    public static implicit operator string(Route route) => route.ToString();
}