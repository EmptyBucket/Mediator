using ConsoleApp5.Pipes;

namespace ConsoleApp5.Bindings;

public readonly record struct PipeBind(Route Route, IPipe Pipe);