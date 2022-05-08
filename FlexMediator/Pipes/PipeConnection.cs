using FlexMediator.Utils;

namespace FlexMediator.Pipes;

public readonly record struct PipeConnection : IAsyncDisposable
{
    private readonly Func<PipeConnection, ValueTask> _disconnect;

    public PipeConnection(Route route, IPipe pipe, Func<PipeConnection, ValueTask> disconnect)
    {
        Route = route;
        Pipe = pipe;
        _disconnect = disconnect;
    }

    public Route Route { get; }

    public IPipe Pipe { get; }

    public ValueTask DisposeAsync() => _disconnect(this);
}