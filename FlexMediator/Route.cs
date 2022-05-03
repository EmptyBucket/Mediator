namespace FlexMediator;

public readonly record struct Route(Type MessageType, Type? ResultType = null, string RoutingKey = "");