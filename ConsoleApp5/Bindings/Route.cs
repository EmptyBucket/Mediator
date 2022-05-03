namespace ConsoleApp5.Bindings;

public readonly record struct Route(Type MessageType, Type? ResultType = null, string RoutingKey = "");