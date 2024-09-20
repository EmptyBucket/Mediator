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

/// <summary>
/// Represents the pipe for request/response messaging model 
/// </summary>
internal class MulticastReqPipe : IMulticastReqPipe
{
    private int _isDisposed;
    private readonly IServiceProvider _serviceProvider;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<Route, ImmutableList<PipeConnection<IReqPipe>>> _pipeConnections = new();

    public MulticastReqPipe(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> ctx,
        CancellationToken cancellationToken = default)
    {
        var pipeConnections = GetPipeConnections(ctx.Route);

        if (!pipeConnections.Any())
            throw new InvalidOperationException(
                $"Message with route: {ctx.Route} must have at least one connected pipe");

        using var scope = _serviceProvider.CreateScope();
        ctx = ctx with { DeliveredAt = DateTime.Now, ServiceProvider = scope.ServiceProvider };

        var tasks = pipeConnections.Select(c => c.Pipe.PassAsync<TMessage, TResult>(ctx, cancellationToken));
        var completedTask = await Task.WhenAny(tasks);
        return completedTask.Result;
    }

    /// <inheritdoc />
    public IEnumerable<PipeConnection<IReqPipe>> GetReqConnections()
    {
        using var scope = _lock.EnterReadScope();
        return _pipeConnections.SelectMany(l => l.Value);
    }

    /// <inheritdoc />
    public PipeConnection<IReqPipe> ConnectOut<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        string connectionName = "")
    {
        var route = Route.For<TMessage, TResult>(routingKey);
        var pipeConnection = ConnectPipe(connectionName, route, pipe);
        return pipeConnection;
    }

    /// <inheritdoc />
    public Task<PipeConnection<IReqPipe>> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        string connectionName = "", CancellationToken cancellationToken = default)
    {
        var route = Route.For<TMessage, TResult>(routingKey);
        var pipeConnection = ConnectPipe(connectionName, route, pipe);
        return Task.FromResult(pipeConnection);
    }

    private IList<PipeConnection<IReqPipe>> GetPipeConnections(Route route)
    {
        using var scope = _lock.EnterReadScope();
        return _pipeConnections.GetValueOrDefault(route) ?? ImmutableList<PipeConnection<IReqPipe>>.Empty;
    }

    private PipeConnection<IReqPipe> ConnectPipe(string connectionName, Route route, IReqPipe pipe)
    {
        var pipeConnection = new PipeConnection<IReqPipe>(connectionName, route, pipe, DisconnectPipe);
        using var scope = _lock.EnterWriteScope();
        _pipeConnections.TryAdd(pipeConnection.Route, ImmutableList<PipeConnection<IReqPipe>>.Empty);
        _pipeConnections[pipeConnection.Route] = _pipeConnections[pipeConnection.Route].Add(pipeConnection);
        return pipeConnection;
    }

    private void DisconnectPipe(PipeConnection<IReqPipe> pipeConnection)
    {
        using var scope = _lock.EnterWriteScope();
        if (_pipeConnections.TryGetValue(pipeConnection.Route, out var list))
        {
            list = list.Remove(pipeConnection);

            if (list.IsEmpty) _pipeConnections.Remove(pipeConnection.Route);
            else _pipeConnections[pipeConnection.Route] = list.Remove(pipeConnection);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) return;

        foreach (var pipeConnection in _pipeConnections.SelectMany(l => l.Value)) await pipeConnection.DisposeAsync();

        _lock.Dispose();
    }
}