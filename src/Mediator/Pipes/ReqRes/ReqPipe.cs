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
        ctx = ctx with { DeliveredAt = DateTime.Now, ServiceProvider = scope.ServiceProvider };
        var pipeConnection = pipeConnections.First();
        return await pipeConnection.Pipe.PassAsync<TMessage, TResult>(ctx, token);
    }

    public IDisposable ConnectOut<TMessage, TResult>(IReqPipe pipe, string routingKey = "")
    {
        var route = Route.For<TMessage, TResult>(routingKey);
        var pipeConnection = ConnectPipe(route, pipe);
        return pipeConnection;
    }

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>(routingKey);
        var pipeConnection = ConnectPipe(route, pipe);
        return Task.FromResult<IAsyncDisposable>(pipeConnection);
    }

    private IList<PipeConnection<IReqPipe>> GetPipeConnections(Route route)
    {
        using var scope = _lock.EnterReadScope();
        return _pipeConnections.GetValueOrDefault(route) ?? ImmutableList<PipeConnection<IReqPipe>>.Empty;
    }

    private PipeConnection<IReqPipe> ConnectPipe(Route route, IReqPipe pipe)
    {
        var pipeConnection = new PipeConnection<IReqPipe>(route, pipe, DisconnectPipe);
        using var scope = _lock.EnterWriteScope();
        _pipeConnections.TryAdd(pipeConnection.Route, ImmutableList<PipeConnection<IReqPipe>>.Empty);
        _pipeConnections[pipeConnection.Route] = _pipeConnections[pipeConnection.Route].Add(pipeConnection);
        return pipeConnection;
    }

    private void DisconnectPipe(PipeConnection<IReqPipe> pipeConnection)
    {
        using var scope = _lock.EnterWriteScope();
        if (_pipeConnections.TryGetValue(pipeConnection.Route, out var l) && l.Remove(pipeConnection).IsEmpty)
            _pipeConnections.Remove(pipeConnection.Route);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.SelectMany(c => c.Value)) await pipeConnection.DisposeAsync();

        _lock.Dispose();
    }
}