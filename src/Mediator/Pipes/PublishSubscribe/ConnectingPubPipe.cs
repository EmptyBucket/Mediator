using Mediator.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Pipes.PublishSubscribe;

internal class ConnectingPubPipe : IConnectingPubPipe
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<Route, List<PipeConnection>> _pipeConnections = new();

    public ConnectingPubPipe(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task PassAsync<TMessage>(MessageContext<TMessage> context, CancellationToken token = default)
    {
        var pipes = GetPipes(context.Route);

        using var scope = _serviceProvider.CreateScope();
        context = context with { DeliveredAt = DateTimeOffset.Now, ServiceProvider = scope.ServiceProvider };
        foreach (var pipe in pipes) pipe.PassAsync(context, token);

        return Task.CompletedTask;
    }

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default)
    {
        var route = Route.For<TMessage>(routingKey);
        var pipeConnection = new PipeConnection(route, pipe, Disconnect);
        Connect(pipeConnection);
        return Task.FromResult((IAsyncDisposable)pipeConnection);
    }

    private IEnumerable<IPubPipe> GetPipes(Route route)
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

        public PipeConnection(Route route, IPubPipe pipe, Func<PipeConnection, ValueTask> disconnect)
        {
            Route = route;
            Pipe = pipe;
            _disconnect = disconnect;
        }

        public Route Route { get; }

        public IPubPipe Pipe { get; }

        public ValueTask DisposeAsync() =>
            Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1 ? ValueTask.CompletedTask : _disconnect(this);
    }
}