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