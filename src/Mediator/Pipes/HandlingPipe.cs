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