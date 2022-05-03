using FlexMediator.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediator.Pipes;

public class HandlerPipe : IPipe
{
    private readonly IHandlerBindProvider _handlerBindProvider;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public HandlerPipe(IHandlerBindProvider handlerBindProvider, IServiceScopeFactory serviceScopeFactory)
    {
        _handlerBindProvider = handlerBindProvider;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options, CancellationToken token)
    {
        var bindings = _handlerBindProvider.GetBindings<TMessage>(options.RoutingKey);

        using var serviceScope = _serviceScopeFactory.CreateScope();
        var serviceProvider = serviceScope.ServiceProvider;
        var handlers = bindings
            .Select(t => t.Handler ?? serviceProvider.GetRequiredService(t.HandlerType!))
            .Cast<IHandler<TMessage>>();
        await Task.WhenAll(handlers.Select(h => h.HandleAsync(message, options, token)));
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token)
    {
        var bindings = _handlerBindProvider.GetBindings<TMessage, TResult>(options.RoutingKey);

        using var serviceScope = _serviceScopeFactory.CreateScope();
        var serviceProvider = serviceScope.ServiceProvider;
        var handlers = bindings
            .Select(t => t.Handler ?? serviceProvider.GetRequiredService(t.HandlerType!))
            .Cast<IHandler<TMessage, TResult>>();
        return await handlers.Single().HandleAsync(message, options, token);
    }
}