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
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Pipes;

/// <summary>
/// Represents the pipe that ends with a handler for publish/subscribe messaging model
/// </summary>
/// <typeparam name="THandlerMessage"></typeparam>
internal class HandlingPipe<THandlerMessage> : IPubPipe
{
    private readonly Func<IServiceProvider, IHandler<THandlerMessage>> _handlerFactory;

    public HandlingPipe(Func<IServiceProvider, IHandler<THandlerMessage>> handlerFactory)
    {
        _handlerFactory = handlerFactory;
    }

    /// <inheritdoc />
    public Task PassAsync<TMessage>(MessageContext<TMessage> ctx, CancellationToken cancellationToken = default)
    {
        if (ctx is not MessageContext<THandlerMessage> handlerMessageContext)
            throw new InvalidOperationException($"Message with route: {ctx.Route} cannot be processed");

        if (ctx.ServiceProvider is null)
            throw new InvalidOperationException($"{nameof(ctx.ServiceProvider)} missing. Handler not constructed");

        using var serviceScope = ctx.ServiceProvider.CreateScope();
        var handler = _handlerFactory(serviceScope.ServiceProvider);
        return handler.HandleAsync(handlerMessageContext, cancellationToken);
    }
}