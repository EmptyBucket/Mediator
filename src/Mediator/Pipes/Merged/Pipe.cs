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
public class Pipe : IConnectingPipe
{
    private int _isDisposed;
    private readonly IConnectingPubPipe _connectingPubPipe;
    private readonly IConnectingReqPipe _connectingReqPipe;

    public Pipe(IServiceProvider serviceProvider)
        : this(new PubPipe(serviceProvider), new ReqPipe(serviceProvider))
    {
    }

    public Pipe(IConnectingPubPipe connectingPubPipe, IConnectingReqPipe connectingReqPipe)
    {
        _connectingPubPipe = connectingPubPipe;
        _connectingReqPipe = connectingReqPipe;
    }

    /// <inheritdoc />
    public Task PassAsync<TMessage>(MessageContext<TMessage> ctx, CancellationToken cancellationToken = default) =>
        _connectingPubPipe.PassAsync(ctx, cancellationToken);

    /// <inheritdoc />
    public Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> ctx,
        CancellationToken cancellationToken = default) =>
        _connectingReqPipe.PassAsync<TMessage, TResult>(ctx, cancellationToken);

    /// <inheritdoc />
    public IEnumerable<PipeConnection<IPubPipe>> GetPubConnections() => _connectingPubPipe.GetPubConnections();

    /// <inheritdoc />
    public PipeConnection<IPubPipe> ConnectOut<TMessage>(IPubPipe pipe, string routingKey = "",
        string connectionName = "", string subscriptionId = "") =>
        _connectingPubPipe.ConnectOut<TMessage>(pipe, routingKey, connectionName, subscriptionId);

    /// <inheritdoc />
    public Task<PipeConnection<IPubPipe>> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string connectionName = "", string subscriptionId = "", CancellationToken cancellationToken = default) =>
        _connectingPubPipe.ConnectOutAsync<TMessage>(pipe, routingKey, connectionName, subscriptionId,
            cancellationToken);

    /// <inheritdoc />
    public IEnumerable<PipeConnection<IReqPipe>> GetReqConnections() => _connectingReqPipe.GetReqConnections();

    /// <inheritdoc />
    public PipeConnection<IReqPipe> ConnectOut<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        string connectionName = "") =>
        _connectingReqPipe.ConnectOut<TMessage, TResult>(pipe, routingKey, connectionName);

    /// <inheritdoc />
    public Task<PipeConnection<IReqPipe>> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        string connectionName = "", CancellationToken cancellationToken = default) =>
        _connectingReqPipe.ConnectOutAsync<TMessage, TResult>(pipe, routingKey, connectionName, cancellationToken);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) return;

        await _connectingPubPipe.DisposeAsync();
        await _connectingReqPipe.DisposeAsync();
    }
}