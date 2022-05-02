namespace ConsoleApp5.Models;

public readonly record struct Route(Type MessageType, string RoutingKey = "");