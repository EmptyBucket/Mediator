namespace Mediator.RequestResponse;

public class HandlingPipe<THandlerMessage, THandlerResult> : IPipe
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