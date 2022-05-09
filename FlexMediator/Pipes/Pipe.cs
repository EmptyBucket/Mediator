using FlexMediator.Utils;

namespace FlexMediator.Pipes;

public class Pipe : IPipe, IPipeConnector
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<Route, List<PipeConnection>> _pipeConnections = new();

    public async Task PassAsync<TMessage>(TMessage message, MessageOptions options,
        CancellationToken token = default)
    {
        var route = Route.For<TMessage>(options.RoutingKey);
        await Task.WhenAll(GetPipes(route).Select(p => p.PassAsync(message, options, token)));
    }

    public async Task<TResult> PassAsync<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>(options.RoutingKey);
        return await GetPipes(route).Single().PassAsync<TMessage, TResult>(message, options, token);
    }

    public Task<PipeConnection> ConnectInAsync<TMessage>(IPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = Route.For<TMessage>(routingKey);
        var pipeConnection = new PipeConnection(route, pipe, Disconnect);
        Connect(pipeConnection);
        return Task.FromResult(pipeConnection);
    }

    public Task<PipeConnection> ConnectInAsync<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>(routingKey);
        var pipeConnection = new PipeConnection(route, pipe, Disconnect);
        Connect(pipeConnection);
        return Task.FromResult(pipeConnection);
    }

    private IEnumerable<IPipe> GetPipes(Route route)
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
}