using ConsoleApp5.Models;
using ConsoleApp5.Pipes;

namespace ConsoleApp5.TransportBindings;

public readonly record struct TransportBind(Route Route, Transport Transport);