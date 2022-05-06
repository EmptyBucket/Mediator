using FlexMediator.Utils;

namespace FlexMediator.Pipes;

public readonly record struct PipeConnection : IAsyncDisposable
{
    private readonly Func<PipeConnection, ValueTask> _disconnect;

    public PipeConnection(Func<PipeConnection, ValueTask> disconnect, Route route, IPipe pipe)
    {
        _disconnect = disconnect;
        Route = route;
        Pipe = pipe;
    }

    public Route Route { get; }

    public IPipe Pipe { get; }

    public ValueTask DisposeAsync() => _disconnect(this);
}