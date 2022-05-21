namespace Mediator.Pipes.Utils;

public record struct PipeConnection<TPipe> : IAsyncDisposable
{
    private int _isDisposed = 0;
    private readonly Func<PipeConnection<TPipe>, ValueTask> _disconnect;

    public PipeConnection(Route route, TPipe pipe, Func<PipeConnection<TPipe>, ValueTask> disconnect)
    {
        Route = route;
        Pipe = pipe;
        _disconnect = disconnect;
    }

    public Route Route { get; }

    public TPipe Pipe { get; }

    public ValueTask DisposeAsync() =>
        Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0 ? _disconnect(this) : ValueTask.CompletedTask;
}