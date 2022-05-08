using FlexMediator.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediator.Pipes;

public class HandlerPipe<THandlerMessage> : IPipe
{
    private readonly Func<IServiceProvider, IHandler<THandlerMessage>> _factory;
    private readonly IServiceProvider _serviceProvider;

    public HandlerPipe(Func<IServiceProvider, IHandler<THandlerMessage>> factory, IServiceProvider serviceProvider)
    {
        _factory = factory;
        _serviceProvider = serviceProvider;
    }

    public Task Handle<TMessage>(TMessage message, MessageOptions options,
        CancellationToken token = default)
    {
        if (message is THandlerMessage handlerMessage)
        {
            using var scope = _serviceProvider.CreateScope();
            var handler = _factory.Invoke(scope.ServiceProvider);
            return handler.HandleAsync(handlerMessage, options, token);
        }

        throw new InvalidOperationException(
            $"Message with {Route.For<TMessage>(options.RoutingKey)} cannot be processed");
    }

    public Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token = default)
    {
        throw new InvalidOperationException(
            $"Message with {Route.For<TMessage, TResult>(options.RoutingKey)} cannot be processed");
    }
}

public class HandlerPipe<THandlerMessage, THandlerResult> : IPipe
{
    private readonly Func<IServiceProvider, IHandler<THandlerMessage, THandlerResult>> _factory;
    private readonly IServiceProvider _serviceProvider;

    public HandlerPipe(Func<IServiceProvider, IHandler<THandlerMessage, THandlerResult>> factory,
        IServiceProvider serviceProvider)
    {
        _factory = factory;
        _serviceProvider = serviceProvider;
    }

    public Task Handle<TMessage>(TMessage message, MessageOptions options,
        CancellationToken token = default)
    {
        throw new InvalidOperationException(
            $"Message with {Route.For<TMessage>(options.RoutingKey)} cannot be processed");
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token = default)
    {
        if (message is THandlerMessage handlerMessage)
        {
            using var scope = _serviceProvider.CreateScope();
            var handler = _factory.Invoke(scope.ServiceProvider);
            return await handler.HandleAsync(handlerMessage, options, token) is TResult result
                ? result
                : throw new InvalidOperationException(
                    $"Message with {Route.For<TMessage>(options.RoutingKey)} cannot be processed");
        }

        throw new InvalidOperationException(
            $"Message with {Route.For<TMessage>(options.RoutingKey)} cannot be processed");
    }
}