// MIT License
// 
// Copyright (c) 2022 Alexey Politov
// https://github.com/EmptyBucket/Mediator
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

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