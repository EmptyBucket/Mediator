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
using Mediator.Pipes.PubSub;
using Mediator.Pipes.RequestResponse;

namespace Mediator;

internal class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnectablePublishPipe _publishPipe;
    private readonly IConnectableSendPipe _sendPipe;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _publishPipe = new PublishPipe();
        _sendPipe = new SendPipe();
    }

    public async Task PublishAsync<TMessage>(TMessage message,
        Action<MessageContext>? contextBuilder = null, CancellationToken token = default)
    {
        var messageContext = new MessageContext(_serviceProvider);
        contextBuilder?.Invoke(messageContext);
        await _publishPipe.PassAsync(message, messageContext, token);
    }

    public async Task<TResult> SendAsync<TMessage, TResult>(TMessage message,
        Action<MessageContext>? contextBuilder = null, CancellationToken token = default)
    {
        var messageContext = new MessageContext(_serviceProvider);
        contextBuilder?.Invoke(messageContext);
        return await _sendPipe.PassAsync<TMessage, TResult>(message, messageContext, token);
    }

    public Task<PublishPipeConnection> ConnectOutAsync<TMessage>(IPublishPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default) =>
        _publishPipe.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionId, token);

    public Task<SendPipeConnection> ConnectOutAsync<TMessage, TResult>(ISendPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        _sendPipe.ConnectOutAsync<TMessage, TResult>(pipe, routingKey, token);

    public ValueTask DisposeAsync() => _publishPipe.DisposeAsync();
}