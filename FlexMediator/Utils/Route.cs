namespace FlexMediator.Utils;

public readonly record struct Route(Type MessageType, string RoutingKey = "", Type? ResultType = null);