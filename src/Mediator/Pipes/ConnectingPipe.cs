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
using Mediator.Pipes.PublishSubscribe;
using Mediator.Pipes.RequestResponse;

namespace Mediator.Pipes;

public class ConnectingPipe : IConnectingPipe
{
    private readonly ConnectingPubPipe _connectingPubPipe;
    private readonly ConnectingReqPipe _connectingReqPipe;

    public ConnectingPipe(IServiceProvider serviceProvider)
    {
        _connectingPubPipe = new ConnectingPubPipe(serviceProvider);
        _connectingReqPipe = new ConnectingReqPipe(serviceProvider);
    }

    public Task PassAsync<TMessage>(MessageContext<TMessage> context, CancellationToken token = default) =>
        _connectingPubPipe.PassAsync(context, token);

    public Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> context,
        CancellationToken token = default) =>
        _connectingReqPipe.PassAsync<TMessage, TResult>(context, token);

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default) =>
        _connectingPubPipe.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionId, token);

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        _connectingReqPipe.ConnectOutAsync<TMessage, TResult>(pipe, routingKey, token);

    public async ValueTask DisposeAsync()
    {
        await _connectingPubPipe.DisposeAsync();
        await _connectingReqPipe.DisposeAsync();
    }
}