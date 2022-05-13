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

namespace Mediator.Pipes.PubSub;

public class HandlingPipe<THandlerMessage> : IPublishPipe
{
    private readonly Func<IServiceProvider, IHandler<THandlerMessage>> _factory;

    public HandlingPipe(Func<IServiceProvider, IHandler<THandlerMessage>> factory)
    {
        _factory = factory;
    }

    public Task PassAsync<TMessage>(TMessage message, MessageContext context,
        CancellationToken token = default)
    {
        var route = Route.For<TMessage>(context.RoutingKey);

        if (message is not THandlerMessage handlerMessage)
            throw new InvalidOperationException($"Message with route: {route} cannot be processed");

        var handler = _factory(context.ServiceProvider);
        return handler.HandleAsync(handlerMessage, context, token);
    }
}