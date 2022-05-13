namespace Mediator.Pipes.PubSub;

public readonly record struct PublishRoute(Type MessageType, string RoutingKey = "")
{
    public static implicit operator string(PublishRoute route) => route.ToString();
}