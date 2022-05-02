using ConsoleApp5.Pipes;

namespace ConsoleApp5.Models;

public readonly record struct Topology(Route Route, IPipe Pipe);