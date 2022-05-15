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

    public Task PassAsync<TMessage>(MessageContext<TMessage> context, CancellationToken token = default)
    {
        if (context is not MessageContext<THandlerMessage> handlerMessageContext)
            throw new InvalidOperationException($"Message with route: {context.Route} cannot be processed");

        var handler = _factory(context.ServiceProvider!);
        return handler.HandleAsync(handlerMessageContext.Message!, handlerMessageContext, token);
    }
}

public class HandlingPipe<THandlerMessage, THandlerResult> : IReqPipe
{
    private readonly Func<IServiceProvider, IHandler<THandlerMessage, THandlerResult>> _factory;

    public HandlingPipe(Func<IServiceProvider, IHandler<THandlerMessage, THandlerResult>> factory)
    {
        _factory = factory;
    }

    public async Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> context,
        CancellationToken token = default)
    {
        if (context is not MessageContext<THandlerMessage> handlerMessageContext ||
            !typeof(THandlerResult).IsAssignableTo(typeof(TResult)))
            throw new InvalidOperationException($"Message with route: {context.Route} cannot be processed");

        var handler = _factory(context.ServiceProvider!);
        var result = await handler.HandleAsync(handlerMessageContext.Message!, handlerMessageContext, token);
        return (TResult)(object)result!;
    }
}