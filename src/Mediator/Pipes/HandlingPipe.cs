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

public class HandlingPipe<THandlerMessage, THandlerResult> : IReqPipe
{
    private readonly Func<IServiceProvider, IHandler<THandlerMessage, THandlerResult>> _factory;

    public HandlingPipe(Func<IServiceProvider, IHandler<THandlerMessage, THandlerResult>> factory)
    {
        _factory = factory;
    }

    public async Task<TResult> PassAsync<TMessage, TResult>(TMessage message, MessageContext context,
        CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>(context.RoutingKey);

        if (message is not THandlerMessage handlerMessage || !typeof(THandlerResult).IsAssignableTo(typeof(TResult)))
            throw new InvalidOperationException($"Message with route: {route} cannot be processed");

        var handler = _factory(context.ServiceProvider);
        var result = await handler.HandleAsync(handlerMessage, context, token);
        return (TResult)(object)result!;
    }
}