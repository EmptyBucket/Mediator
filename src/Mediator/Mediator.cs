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
using Mediator.Pipes;

namespace Mediator;

internal class Mediator : IMediator
{
    private readonly ConnectingPipe _dispatchPipe;
    private readonly ConnectingPipe _receivePipe;

    public Mediator(ConnectingPipe dispatchPipe, ConnectingPipe receivePipe)
    {
        _dispatchPipe = dispatchPipe;
        _receivePipe = receivePipe;
    }

    public async Task PublishAsync<TMessage>(TMessage message,
        Func<MessageContext<TMessage>, MessageContext<TMessage>>? ctxBuilder = null, CancellationToken token = default)
    {
        var route = Route.For<TMessage>();
        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var ctx = new MessageContext<TMessage>(route, messageId, correlationId, DateTimeOffset.Now, message);
        ctxBuilder?.Invoke(ctx);
        await _dispatchPipe.PassAsync(ctx, token);
    }

    public async Task<TResult> SendAsync<TMessage, TResult>(TMessage message,
        Func<MessageContext<TMessage>, MessageContext<TMessage>>? ctxBuilder = null, CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>();
        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var ctx = new MessageContext<TMessage>(route, messageId, correlationId, DateTimeOffset.Now, message);
        ctxBuilder?.Invoke(ctx);
        return await _dispatchPipe.PassAsync<TMessage, TResult>(ctx, token);
    }

    public (IConnectingPipe Dispatch, IConnectingPipe Receive) Topology => (_dispatchPipe, _receivePipe);

    public async ValueTask DisposeAsync()
    {
        await _dispatchPipe.DisposeAsync();
        await _receivePipe.DisposeAsync();
    }
}