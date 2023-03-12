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

namespace Mediator.Pipes;

/// <summary>
/// Represents the pipe that ends with a handler for request/response messaging model
/// </summary>
/// <typeparam name="THandlerMessage"></typeparam>
/// <typeparam name="THandlerResult"></typeparam>
internal class HandlingPipe<THandlerMessage, THandlerResult> : IReqPipe
{
    private readonly Func<IServiceProvider, IHandler<THandlerMessage, THandlerResult>> _handlingFactory;

    public HandlingPipe(Func<IServiceProvider, IHandler<THandlerMessage, THandlerResult>> handlingFactory)
    {
        _handlingFactory = handlingFactory;
    }

    /// <inheritdoc />
    public async Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> ctx,
        CancellationToken cancellationToken = default)
    {
        if (ctx is not MessageContext<THandlerMessage> handlerCtx ||
            !typeof(TResult).IsAssignableFrom(typeof(THandlerResult)))
            throw new InvalidOperationException($"Message with route: {ctx.Route} cannot be processed");

        if (ctx.ServiceProvider is null)
            throw new InvalidOperationException($"{nameof(ctx.ServiceProvider)} missing. Handler not constructed");

        var handler = _handlingFactory(ctx.ServiceProvider);
        var result = await handler.HandleAsync(handlerCtx, cancellationToken);
        return (TResult)(object)result!;
    }
}