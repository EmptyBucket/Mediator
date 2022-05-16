using Mediator.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Pipes.RequestResponse;

internal class ConnectingReqPipe : IConnectingReqPipe
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<Route, List<PipeConnection>> _pipeConnections = new();

    public ConnectingReqPipe(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> ctx,
        CancellationToken token = default)
    {
        var pipes = GetPipes(ctx.Route);

        if (pipes.Length != 1)
            throw new InvalidOperationException($"Message with route: {ctx.Route} must have only one registered pipe");

        using var scope = _serviceProvider.CreateScope();
        ctx = ctx with { DeliveredAt = DateTimeOffset.Now, ServiceProvider = scope.ServiceProvider};
        return await pipes.First().PassAsync<TMessage, TResult>(ctx, token);
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
        if (_pipeConnections.TryGetValue(pipeConnection.Route, out var l) && l.Remove(pipeConnection) && !l.Any())
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

        public ValueTask DisposeAsync() =>
            Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1 ? ValueTask.CompletedTask : _disconnect(this);
    }
}