namespace Mediator.RequestResponse;

public record struct PipeConnection : IAsyncDisposable
{
    private int _isDisposed = 0;
    private readonly Func<PipeConnection, ValueTask> _disconnect;

    public PipeConnection(Route route, IPipe pipe, Func<PipeConnection, ValueTask> disconnect)
    {
        Route = route;
        Pipe = pipe;
        _disconnect = disconnect;
    }

    public Route Route { get; }

    public IPipe Pipe { get; }

    public ValueTask DisposeAsync() => Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1
        ? ValueTask.CompletedTask
        : _disconnect(this);
}