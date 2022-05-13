using Mediator.Handlers;

namespace Mediator.Pipes.RequestResponse;

public class SendPipe : IConnectableSendPipe
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<SendRoute, List<SendPipeConnection>> _pipeConnections = new();

    public async Task<TResult> PassAsync<TMessage, TResult>(TMessage message, MessageContext context,
        CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>(context.RoutingKey);
        var pipes = GetPipes(route);
        
        if (!pipes.Any()) throw new InvalidOperationException($"Message with route: {route} has not registered pipes");
        
        return await pipes.First().PassAsync<TMessage, TResult>(message, context, token);
    }

    public Task<SendPipeConnection> ConnectOutAsync<TMessage, TResult>(ISendPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>(routingKey);
        var pipeConnection = new SendPipeConnection(route, pipe, Disconnect);
        Connect(pipeConnection);
        return Task.FromResult(pipeConnection);
    }

    private ISendPipe[] GetPipes(SendRoute route)
    {
        _lock.EnterReadLock();
        var pipeConnections = _pipeConnections.GetValueOrDefault(route) ?? Enumerable.Empty<SendPipeConnection>();
        var pipes = pipeConnections.Select(c => c.Pipe).ToArray();
        _lock.ExitReadLock();
        return pipes;
    }

    private void Connect(SendPipeConnection pipeConnection)
    {
        _lock.EnterWriteLock();
        _pipeConnections.TryAdd(pipeConnection.Route, new List<SendPipeConnection>());
        _pipeConnections[pipeConnection.Route].Add(pipeConnection);
        _lock.ExitWriteLock();
    }

    private ValueTask Disconnect(SendPipeConnection pipeConnection)
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