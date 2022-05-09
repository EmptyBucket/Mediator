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

using FlexMediator.Pipes;
using FlexMediator.Utils;

namespace FlexMediator;

public class Mediator : IMediator, IPipeConnector
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Pipe _pipe;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _pipe = new Pipe();
    }

    public async Task PublishAsync<TMessage>(TMessage message,
        Action<MessageOptions>? optionsBuilder = null, CancellationToken token = default)
    {
        var messageOptions = new MessageOptions(_serviceProvider);
        optionsBuilder?.Invoke(messageOptions);
        await _pipe.PassAsync(message, messageOptions, token);
    }

    public async Task<TResult> SendAsync<TMessage, TResult>(TMessage message,
        Action<MessageOptions>? optionsBuilder = null, CancellationToken token = default)
    {
        var messageOptions = new MessageOptions(_serviceProvider);
        optionsBuilder?.Invoke(messageOptions);
        return await _pipe.PassAsync<TMessage, TResult>(message, messageOptions, token);
    }

    public Task<PipeConnection> ConnectInAsync<TMessage>(IPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        _pipe.ConnectInAsync<TMessage>(pipe, routingKey, token);

    public Task<PipeConnection> ConnectInAsync<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        _pipe.ConnectInAsync<TMessage, TResult>(pipe, routingKey, token);

    public ValueTask DisposeAsync() => _pipe.DisposeAsync();
}