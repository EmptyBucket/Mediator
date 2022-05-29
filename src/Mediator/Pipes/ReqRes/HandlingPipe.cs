using Mediator.Handlers;

namespace Mediator.Pipes;

internal class HandlingPipe<THandlerMessage, THandlerResult> : IReqPipe
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
            !typeof(TResult).IsAssignableFrom(typeof(THandlerResult)))
            throw new InvalidOperationException($"Message with route: {ctx.Route} cannot be processed");

        if (ctx.ServiceProvider is null)
            throw new InvalidOperationException($"{nameof(ctx.ServiceProvider)} missing. Handler not constructed");

        var handler = _factory(ctx.ServiceProvider);
        var result = await handler.HandleAsync(handlerCtx, token);
        return (TResult)(object)result!;
    }
}