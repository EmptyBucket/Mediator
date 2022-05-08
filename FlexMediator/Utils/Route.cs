namespace FlexMediator.Utils;

public readonly record struct Route(Type MessageType, string RoutingKey = "", Type? ResultType = null)
{
    public static Route For<TMessage>(string routingKey = "") =>
        new(typeof(TMessage), routingKey);

    public static Route For<TMessage, TResult>(string routingKey = "") =>
        new(typeof(TMessage), routingKey, typeof(TResult));

    public static implicit operator string(Route route) => route.ToString();
};