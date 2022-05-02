namespace ConsoleApp5.Bindings;

public readonly record struct Route(Type MessageType, string RoutingKey = "");