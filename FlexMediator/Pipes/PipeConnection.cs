using FlexMediator.Utils;

namespace FlexMediator.Pipes;

public record PipeConnection : IAsyncDisposable
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

public record PipeConnection<TPipe> : PipeConnection
    where TPipe : IPipe
{
    public PipeConnection(Func<PipeConnection, ValueTask> disconnect, Route route, TPipe pipe)
        : base(disconnect, route, pipe)
    {
        Pipe = pipe;
    }

    public new TPipe Pipe { get; }
}