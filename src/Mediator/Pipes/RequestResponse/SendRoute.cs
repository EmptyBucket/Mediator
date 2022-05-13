namespace Mediator.Pipes.RequestResponse;

public readonly record struct SendRoute(Type MessageType, Type ResultType, string RoutingKey = "")
{
    public static implicit operator string(SendRoute route) => route.ToString();
}