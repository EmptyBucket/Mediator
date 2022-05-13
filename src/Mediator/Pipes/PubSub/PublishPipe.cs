using Mediator.Handlers;

namespace Mediator.Pipes.PubSub;

public class PublishPipe : IConnectablePublishPipe
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<PublishRoute, List<PublishPipeConnection>> _pipeConnections = new();

    public async Task PassAsync<TMessage>(TMessage message, MessageContext context,
        CancellationToken token = default)
    {
        var route = Route.For<TMessage>(context.RoutingKey);
        var pipes = GetPipes(route);
        await Task.WhenAll(pipes.Select(p => p.PassAsync(message, context, token)));
    }

    public Task<PublishPipeConnection> ConnectOutAsync<TMessage>(IPublishPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default)
    {
        var route = Route.For<TMessage>(routingKey);
        var pipeConnection = new PublishPipeConnection(route, pipe, Disconnect);
        Connect(pipeConnection);
        return Task.FromResult(pipeConnection);
    }

    private IEnumerable<IPublishPipe> GetPipes(PublishRoute route)
    {
        _lock.EnterReadLock();
        var pipeConnections = _pipeConnections.GetValueOrDefault(route) ?? Enumerable.Empty<PublishPipeConnection>();
        var pipes = pipeConnections.Select(c => c.Pipe).ToArray();
        _lock.ExitReadLock();
        return pipes;
    }

    private void Connect(PublishPipeConnection pipeConnection)
    {
        _lock.EnterWriteLock();
        _pipeConnections.TryAdd(pipeConnection.Route, new List<PublishPipeConnection>());
        _pipeConnections[pipeConnection.Route].Add(pipeConnection);
        _lock.ExitWriteLock();
    }

    private ValueTask Disconnect(PublishPipeConnection pipeConnection)
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
}