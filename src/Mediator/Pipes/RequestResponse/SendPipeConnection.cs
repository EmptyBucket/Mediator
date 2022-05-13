namespace Mediator.Pipes.RequestResponse;

public record struct SendPipeConnection : IAsyncDisposable
{
    private int _isDisposed = 0;
    private readonly Func<SendPipeConnection, ValueTask> _disconnect;

    public SendPipeConnection(SendRoute route, ISendPipe pipe, Func<SendPipeConnection, ValueTask> disconnect)
    {
        Route = route;
        Pipe = pipe;
        _disconnect = disconnect;
    }

    public SendRoute Route { get; }

    public ISendPipe Pipe { get; }

    public ValueTask DisposeAsync() => Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1
        ? ValueTask.CompletedTask
        : _disconnect(this);
}