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

using Mediator.Handlers;
using Mediator.Pipes.Utils;

namespace Mediator.Pipes;

/// <summary>
/// Represents the pipe for publish/subscribe and request/response messaging model 
/// </summary>
public class BroadcastPipe : IBroadcastPipe
{
    private int _isDisposed;
    private readonly IBroadcastPubPipe _broadcastPubPipe;
    private readonly IBroadcastReqPipe _broadcastReqPipe;

    public BroadcastPipe(IServiceProvider serviceProvider) : this(new BroadcastPubPipe(serviceProvider),
        new BroadcastReqPipe(serviceProvider))
    {
    }

    public BroadcastPipe(IBroadcastPubPipe broadcastPubPipe, IBroadcastReqPipe broadcastReqPipe)
    {
        _broadcastPubPipe = broadcastPubPipe;
        _broadcastReqPipe = broadcastReqPipe;
    }

    /// <inheritdoc />
    public virtual Task PassAsync<TMessage>(MessageContext<TMessage> ctx,
        CancellationToken cancellationToken = default) =>
        _broadcastPubPipe.PassAsync(ctx, cancellationToken);

    /// <inheritdoc />
    public virtual Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> ctx,
        CancellationToken cancellationToken = default) =>
        _broadcastReqPipe.PassAsync<TMessage, TResult>(ctx, cancellationToken);

    /// <inheritdoc />
    public IEnumerable<PipeConnection<IPubPipe>> GetPubConnections() => _broadcastPubPipe.GetPubConnections();

    /// <inheritdoc />
    public virtual PipeConnection<IPubPipe> ConnectOut(IPubPipe pipe, string connectionName = "",
        string subscriptionId = "") =>
        _broadcastPubPipe.ConnectOut(pipe, connectionName, subscriptionId);

    /// <inheritdoc />
    public virtual Task<PipeConnection<IPubPipe>> ConnectOutAsync(IPubPipe pipe, string connectionName = "",
        string subscriptionId = "", CancellationToken cancellationToken = default) =>
        _broadcastPubPipe.ConnectOutAsync(pipe, connectionName, subscriptionId,
            cancellationToken);

    /// <inheritdoc />
    public IEnumerable<PipeConnection<IReqPipe>> GetReqConnections() => _broadcastReqPipe.GetReqConnections();

    /// <inheritdoc />
    public virtual PipeConnection<IReqPipe> ConnectOut(IReqPipe pipe, string connectionName = "") =>
        _broadcastReqPipe.ConnectOut(pipe, connectionName);

    /// <inheritdoc />
    public virtual Task<PipeConnection<IReqPipe>> ConnectOutAsync(IReqPipe pipe, string connectionName = "",
        CancellationToken cancellationToken = default) =>
        _broadcastReqPipe.ConnectOutAsync(pipe, connectionName, cancellationToken);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) return;

        await _broadcastPubPipe.DisposeAsync();
        await _broadcastReqPipe.DisposeAsync();
    }
}