using System.Collections.Immutable;
using Mediator.Handlers;
using Mediator.Pipes.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Pipes;

internal class ReqPipe : IConnectingReqPipe
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<Route, ImmutableList<PipeConnection<IReqPipe>>> _pipeConnections = new();

    public ReqPipe(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> ctx,
        CancellationToken token = default)
    {
        var pipeConnections = GetPipeConnections(ctx.Route);

        if (pipeConnections.Count != 1)
            throw new InvalidOperationException($"Message with route: {ctx.Route} must have only one registered pipe");

        using var scope = _serviceProvider.CreateScope();
        var pipeConnection = pipeConnections.First();
        ctx = ctx with { DeliveredAt = DateTimeOffset.Now, ServiceProvider = scope.ServiceProvider };
        return await pipeConnection.Pipe.PassAsync<TMessage, TResult>(ctx, token);
    }

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>(routingKey);
        var pipeConnection = new PipeConnection<IReqPipe>(route, pipe, Disconnect);
        Connect(pipeConnection);
        return Task.FromResult((IAsyncDisposable)pipeConnection);
    }

    private IList<PipeConnection<IReqPipe>> GetPipeConnections(Route route)
    {
        try
        {
            _lock.EnterReadLock();
            return _pipeConnections.GetValueOrDefault(route) ?? ImmutableList<PipeConnection<IReqPipe>>.Empty;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private void Connect(PipeConnection<IReqPipe> pipeConnection)
    {
        try
        {
            _lock.EnterWriteLock();
            var list = ImmutableList<PipeConnection<IReqPipe>>.Empty;
            _pipeConnections.TryAdd(pipeConnection.Route, list);
            _pipeConnections[pipeConnection.Route] = list.Add(pipeConnection);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private ValueTask Disconnect(PipeConnection<IReqPipe> pipeConnection)
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