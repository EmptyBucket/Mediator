using System.Collections.Immutable;
using Mediator.Handlers;
using Mediator.Pipes.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Pipes;

internal class PubPipe : IConnectingPubPipe
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<Route, ImmutableList<PipeConnection<IPubPipe>>> _pipeConnections = new();

    public PubPipe(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task PassAsync<TMessage>(MessageContext<TMessage> ctx, CancellationToken token = default)
    {
        var pipeConnections = GetPipeConnections(ctx.Route);

        using var scope = _serviceProvider.CreateScope();
        ctx = ctx with { DeliveredAt = DateTimeOffset.Now, ServiceProvider = scope.ServiceProvider };

        foreach (var pipeConnection in pipeConnections) pipeConnection.Pipe.PassAsync(ctx, token);

        return Task.CompletedTask;
    }

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default)
    {
        var route = Route.For<TMessage>(routingKey);
        var pipeConnection = new PipeConnection<IPubPipe>(route, pipe, Disconnect);
        Connect(pipeConnection);
        return Task.FromResult((IAsyncDisposable)pipeConnection);
    }

    private IEnumerable<PipeConnection<IPubPipe>> GetPipeConnections(Route route)
    {
        try
        {
            _lock.EnterReadLock();
            return _pipeConnections.GetValueOrDefault(route) ?? Enumerable.Empty<PipeConnection<IPubPipe>>();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private void Connect(PipeConnection<IPubPipe> pipeConnection)
    {
        try
        {
            _lock.EnterWriteLock();
            var list = ImmutableList<PipeConnection<IPubPipe>>.Empty;
            _pipeConnections.TryAdd(pipeConnection.Route, list);
            _pipeConnections[pipeConnection.Route] = _pipeConnections[pipeConnection.Route].Add(pipeConnection);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private ValueTask Disconnect(PipeConnection<IPubPipe> pipeConnection)
    {
        try
        {
            _lock.EnterWriteLock();
            if (_pipeConnections.TryGetValue(pipeConnection.Route, out var l) && l.Remove(pipeConnection).IsEmpty)
                _pipeConnections.Remove(pipeConnection.Route);
            return ValueTask.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.SelectMany(c => c.Value)) await pipeConnection.DisposeAsync();
    }
}