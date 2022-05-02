using ConsoleApp5.Bindings;
using ConsoleApp5.Pipes;

namespace ConsoleApp5.Topologies;

public readonly record struct Topology(Route Route, IPipe Pipe);