using Mediator.Handlers;

namespace Mediator.Pipes.RequestResponse;

internal class ConnectingReqPipe : IConnectingReqPipe
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<Route, List<PipeConnection>> _pipeConnections = new();

    public async Task<TResult> PassAsync<TMessage, TResult>(TMessage message, MessageContext context,
        CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>(context.RoutingKey);
        var pipes = GetPipes(route);

        if (!pipes.Any()) throw new InvalidOperationException($"Message with route: {route} has not registered pipes");

        return await pipes.First().PassAsync<TMessage, TResult>(message, context, token);
    }

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>(routingKey);
        var pipeConnection = new PipeConnection(route, pipe, Disconnect);
        Connect(pipeConnection);
        return Task.FromResult((IAsyncDisposable)pipeConnection);
    }

    private IReqPipe[] GetPipes(Route route)
    {
        _lock.EnterReadLock();
        var pipeConnections = _pipeConnections.GetValueOrDefault(route) ?? Enumerable.Empty<PipeConnection>();
        var pipes = pipeConnections.Select(c => c.Pipe).ToArray();
        _lock.ExitReadLock();
        return pipes;
    }

    private void Connect(PipeConnection pipeConnection)
    {
        _lock.EnterWriteLock();
        _pipeConnections.TryAdd(pipeConnection.Route, new List<PipeConnection>());
        _pipeConnections[pipeConnection.Route].Add(pipeConnection);
        _lock.ExitWriteLock();
    }

    private ValueTask Disconnect(PipeConnection pipeConnection)
    {
        _lock.EnterWriteLock();
        if (_pipeConnections.TryGetValue(pipeConnection.Route, out var list) &&
            list.Remove(pipeConnection) && !list.Any())
            _pipeConnections.Remove(pipeConnection.Route);
        _lock.ExitWriteLock();

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.SelectMany(c => c.Value)) await pipeConnection.DisposeAsync();
    }

    private record struct PipeConnection : IAsyncDisposable
    {
        private int _isDisposed = 0;
        private readonly Func<PipeConnection, ValueTask> _disconnect;

        public PipeConnection(Route route, IReqPipe pipe, Func<PipeConnection, ValueTask> disconnect)
        {
            Route = route;
            Pipe = pipe;
            _disconnect = disconnect;
        }

        public Route Route { get; }

        public IReqPipe Pipe { get; }

        public ValueTask DisposeAsync() => Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1
            ? ValueTask.CompletedTask
            : _disconnect(this);
    }
}