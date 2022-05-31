namespace Mediator.Pipes.Utils;

public record struct PipeConnection<TPipe> : IDisposable, IAsyncDisposable
{
    private int _isDisposed = 0;
    private readonly Action<PipeConnection<TPipe>> _disconnect;
    private readonly Func<PipeConnection<TPipe>, ValueTask> _disconnectAsync;

    public PipeConnection(Route route, TPipe pipe,
        Action<PipeConnection<TPipe>> disconnect, Func<PipeConnection<TPipe>, ValueTask> disconnectAsync)
    {
        Route = route;
        Pipe = pipe;
        _disconnect = disconnect;
        _disconnectAsync = disconnectAsync;
    }

    public PipeConnection(Route route, TPipe pipe, Action<PipeConnection<TPipe>> disconnect)
        : this(route, pipe, disconnect, DisconnectAsync(disconnect))
    {
    }

    private static Func<PipeConnection<TPipe>, ValueTask> DisconnectAsync(Action<PipeConnection<TPipe>> disconnect) =>
        p =>
        {
            disconnect(p);
            return new ValueTask();
        };

    public Route Route { get; }

    public TPipe Pipe { get; }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0) _disconnect(this);
    }

    public ValueTask DisposeAsync() =>
        Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0 ? _disconnectAsync(this) : new ValueTask();
}