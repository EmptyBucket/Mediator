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

public class HandlingPipe<THandlerMessage> : IPubPipe
{
    private readonly Func<IServiceProvider, IHandler<THandlerMessage>> _factory;

    public HandlingPipe(Func<IServiceProvider, IHandler<THandlerMessage>> factory)
    {
        _factory = factory;
    }

    public Task PassAsync<TMessage>(MessageContext<TMessage> ctx, CancellationToken token = default)
    {
        if (ctx is not MessageContext<THandlerMessage> handlerMessageContext)
            throw new InvalidOperationException($"Message with route: {ctx.Route} cannot be processed");

        if (ctx.ServiceProvider is null)
            throw new InvalidOperationException($"{nameof(ctx.ServiceProvider)} missing. Handler not constructed");

        var handler = _factory(ctx.ServiceProvider);
        return handler.HandleAsync(handlerMessageContext, token);
    }
}

public class HandlingPipe<THandlerMessage, THandlerResult> : IReqPipe
{
    private readonly Func<IServiceProvider, IHandler<THandlerMessage, THandlerResult>> _factory;

    public HandlingPipe(Func<IServiceProvider, IHandler<THandlerMessage, THandlerResult>> factory)
    {
        _factory = factory;
    }

    public async Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> ctx,
        CancellationToken token = default)
    {
        if (ctx is not MessageContext<THandlerMessage> handlerCtx ||
            !typeof(THandlerResult).IsAssignableTo(typeof(TResult)))
            throw new InvalidOperationException($"Message with route: {ctx.Route} cannot be processed");

        if (ctx.ServiceProvider is null)
            throw new InvalidOperationException($"{nameof(ctx.ServiceProvider)} missing. Handler not constructed");

        var handler = _factory(ctx.ServiceProvider);
        var result = await handler.HandleAsync(handlerCtx, token);
        return (TResult)(object)result!;
    }
}