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

using FlexMediator.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediator.Pipes;

public class HandlerPipe<THandlerMessage> : IPipe
{
    private readonly Func<IServiceProvider, IHandler<THandlerMessage>> _factory;
    private readonly IServiceProvider _serviceProvider;

    public HandlerPipe(Func<IServiceProvider, IHandler<THandlerMessage>> factory, IServiceProvider serviceProvider)
    {
        _factory = factory;
        _serviceProvider = serviceProvider;
    }

    public Task Handle<TMessage>(TMessage message, MessageOptions options,
        CancellationToken token = default)
    {
        if (message is THandlerMessage handlerMessage)
        {
            using var scope = _serviceProvider.CreateScope();
            var handler = _factory.Invoke(scope.ServiceProvider);
            return handler.HandleAsync(handlerMessage, options, token);
        }

        throw new InvalidOperationException(
            $"Message with {Route.For<TMessage>(options.RoutingKey)} cannot be processed");
    }

    public Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token = default)
    {
        throw new InvalidOperationException(
            $"Message with {Route.For<TMessage, TResult>(options.RoutingKey)} cannot be processed");
    }
}

public class HandlerPipe<THandlerMessage, THandlerResult> : IPipe
{
    private readonly Func<IServiceProvider, IHandler<THandlerMessage, THandlerResult>> _factory;
    private readonly IServiceProvider _serviceProvider;

    public HandlerPipe(Func<IServiceProvider, IHandler<THandlerMessage, THandlerResult>> factory,
        IServiceProvider serviceProvider)
    {
        _factory = factory;
        _serviceProvider = serviceProvider;
    }

    public Task Handle<TMessage>(TMessage message, MessageOptions options,
        CancellationToken token = default)
    {
        throw new InvalidOperationException(
            $"Message with {Route.For<TMessage>(options.RoutingKey)} cannot be processed");
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token = default)
    {
        if (message is THandlerMessage handlerMessage)
        {
            using var scope = _serviceProvider.CreateScope();
            var handler = _factory.Invoke(scope.ServiceProvider);
            return await handler.HandleAsync(handlerMessage, options, token) is TResult result
                ? result
                : throw new InvalidOperationException(
                    $"Message with {Route.For<TMessage>(options.RoutingKey)} cannot be processed");
        }

        throw new InvalidOperationException(
            $"Message with {Route.For<TMessage>(options.RoutingKey)} cannot be processed");
    }
}