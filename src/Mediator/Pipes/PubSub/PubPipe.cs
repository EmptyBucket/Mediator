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
        ctx = ctx with { DeliveredAt = DateTime.Now, ServiceProvider = scope.ServiceProvider };

        foreach (var pipeConnection in pipeConnections) pipeConnection.Pipe.PassAsync(ctx, token);

        return Task.CompletedTask;
    }

    public IDisposable ConnectOut<TMessage>(IPubPipe pipe, string routingKey = "", string subscriptionId = "")
    {
        var route = Route.For<TMessage>(routingKey);
        var pipeConnection = ConnectPipe(route, pipe);
        return pipeConnection;
    }

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default)
    {
        var route = Route.For<TMessage>(routingKey);
        var pipeConnection = ConnectPipe(route, pipe);
        return Task.FromResult<IAsyncDisposable>(pipeConnection);
    }

    private IEnumerable<PipeConnection<IPubPipe>> GetPipeConnections(Route route)
    {
        using var scope = _lock.EnterReadScope();
        return _pipeConnections.GetValueOrDefault(route) ?? Enumerable.Empty<PipeConnection<IPubPipe>>();
    }

    private PipeConnection<IPubPipe> ConnectPipe(Route route, IPubPipe pipe)
    {
        var pipeConnection = new PipeConnection<IPubPipe>(route, pipe, DisconnectPipe);
        using var scope = _lock.EnterWriteScope();
        _pipeConnections.TryAdd(pipeConnection.Route, ImmutableList<PipeConnection<IPubPipe>>.Empty);
        _pipeConnections[pipeConnection.Route] = _pipeConnections[pipeConnection.Route].Add(pipeConnection);
        return pipeConnection;
    }

    private void DisconnectPipe(PipeConnection<IPubPipe> pipeConnection)
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