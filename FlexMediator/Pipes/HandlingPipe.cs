using FlexMediator.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediator.Pipes;

public class HandlingPipe<THandlerMessage> : IPipe
{
    private readonly Func<IServiceProvider, IHandler<THandlerMessage>> _factory;
    private readonly IServiceProvider _serviceProvider;

    public HandlingPipe(Func<IServiceProvider, IHandler<THandlerMessage>> factory, IServiceProvider serviceProvider)
    {
        _factory = factory;
        _serviceProvider = serviceProvider;
    }

    public Task PassAsync<TMessage>(TMessage message, MessageContext context,
        CancellationToken token = default)
    {
        if (message is THandlerMessage handlerMessage)
        {
            var handler = _factory(_serviceProvider);
            return handler.HandleAsync(handlerMessage, context, token);
        }

        throw new InvalidOperationException(
            $"Message with {Route.For<TMessage>(context.RoutingKey)} cannot be processed");
    }

    public Task<TResult> PassAsync<TMessage, TResult>(TMessage message, MessageContext context,
        CancellationToken token = default)
    {
        throw new InvalidOperationException(
            $"Message with {Route.For<TMessage, TResult>(context.RoutingKey)} cannot be processed");
    }
}

public class HandlingPipe<THandlerMessage, THandlerResult> : IPipe
{
    private readonly Func<IServiceProvider, IHandler<THandlerMessage, THandlerResult>> _factory;
    private readonly IServiceProvider _serviceProvider;

    public HandlingPipe(Func<IServiceProvider, IHandler<THandlerMessage, THandlerResult>> factory,
        IServiceProvider serviceProvider)
    {
        _factory = factory;
        _serviceProvider = serviceProvider;
    }

    public Task PassAsync<TMessage>(TMessage message, MessageContext context,
        CancellationToken token = default)
    {
        throw new InvalidOperationException(
            $"Message with {Route.For<TMessage>(context.RoutingKey)} cannot be processed");
    }

    public async Task<TResult> PassAsync<TMessage, TResult>(TMessage message, MessageContext context,
        CancellationToken token = default)
    {
        if (message is THandlerMessage handlerMessage)
        {
            using var scope = _serviceProvider.CreateScope();
            var handler = _factory(scope.ServiceProvider);
            return await handler.HandleAsync(handlerMessage, context, token) is TResult result
                ? result
                : throw new InvalidOperationException(
                    $"Message with {Route.For<TMessage>(context.RoutingKey)} cannot be processed");
        }

        throw new InvalidOperationException(
            $"Message with {Route.For<TMessage>(context.RoutingKey)} cannot be processed");
    }
}