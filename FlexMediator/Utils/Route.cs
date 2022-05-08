namespace FlexMediator.Utils;

public readonly record struct Route(Type MessageType, string RoutingKey = "", Type? ResultType = null)
{
    public static implicit operator string(Route route) => route.ToString();
};