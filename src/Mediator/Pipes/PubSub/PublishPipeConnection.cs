namespace Mediator.Pipes.PubSub;

public record struct PublishPipeConnection : IAsyncDisposable
{
    private int _isDisposed = 0;
    private readonly Func<PublishPipeConnection, ValueTask> _disconnect;

    public PublishPipeConnection(PublishRoute route, IPublishPipe pipe,
        Func<PublishPipeConnection, ValueTask> disconnect)
    {
        Route = route;
        Pipe = pipe;
        _disconnect = disconnect;
    }

    public PublishRoute Route { get; }

    public IPublishPipe Pipe { get; }

    public ValueTask DisposeAsync() => Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1
        ? ValueTask.CompletedTask
        : _disconnect(this);
}