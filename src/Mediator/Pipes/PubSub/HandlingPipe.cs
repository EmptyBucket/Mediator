namespace Mediator.PubSub;

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
        var route = Route.For<TMessage>(context.RoutingKey);

        if (message is not THandlerMessage handlerMessage)
            throw new InvalidOperationException($"Message with route: {route} cannot be processed");

        var handler = _factory(context.ServiceProvider);
        return handler.HandleAsync(handlerMessage, context, token);
    }
}