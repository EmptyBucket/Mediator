namespace Mediator.Pipes;

public class HandlingPipe<THandlerMessage> : IPipe
{
    private readonly Func<IServiceProvider, IHandler<THandlerMessage>> _factory;

    public HandlingPipe(Func<IServiceProvider, IHandler<THandlerMessage>> factory)
    {
        _factory = factory;
    }

    public Task PassAsync<TMessage>(TMessage message, MessageContext context,
        CancellationToken token = default)
    {
        if (message is not THandlerMessage handlerMessage)
            throw new MessageCannotBeProcessedException(Route.For<TMessage>(context.RoutingKey));

        var handler = _factory(context.ServiceProvider);
        return handler.HandleAsync(handlerMessage, context, token);
    }

    public Task<TResult> PassAsync<TMessage, TResult>(TMessage message, MessageContext context,
        CancellationToken token = default) =>
        throw new MessageCannotBeProcessedException(Route.For<TMessage, TResult>(context.RoutingKey));
}

public class HandlingPipe<THandlerMessage, THandlerResult> : IPipe
{
    private readonly Func<IServiceProvider, IHandler<THandlerMessage, THandlerResult>> _factory;

    public HandlingPipe(Func<IServiceProvider, IHandler<THandlerMessage, THandlerResult>> factory)
    {
        _factory = factory;
    }

    public Task PassAsync<TMessage>(TMessage message, MessageContext context,
        CancellationToken token = default) =>
        throw new MessageCannotBeProcessedException(Route.For<TMessage>(context.RoutingKey));

    public async Task<TResult> PassAsync<TMessage, TResult>(TMessage message, MessageContext context,
        CancellationToken token = default)
    {
        var route = Route.For<TMessage>(context.RoutingKey);

        if (message is not THandlerMessage handlerMessage || !typeof(THandlerResult).IsAssignableTo(typeof(TResult)))
            throw new MessageCannotBeProcessedException(route);

        var handler = _factory(context.ServiceProvider);
        var result = await handler.HandleAsync(handlerMessage, context, token);
        return (TResult)(object)result!;
    }
}

public class MessageCannotBeProcessedException : InvalidOperationException
{
    public MessageCannotBeProcessedException(Route route) : base($"Message with route: {route} cannot be processed")
    {
    }
}