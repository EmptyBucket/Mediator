using FlexMediator.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediator.Pipes;

public class HandlerPipe : IPipe
{
    private readonly IHandlerBindings _handlerBindings;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public HandlerPipe(IHandlerBindings handlerBindings, IServiceScopeFactory serviceScopeFactory)
    {
        _handlerBindings = handlerBindings;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options, CancellationToken token)
    {
        var route = new Route(typeof(TMessage), RoutingKey: options.RoutingKey);
        var bindings = _handlerBindings.GetValueOrDefault(route) ?? Enumerable.Empty<HandlerBind>();

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
        var route = new Route(typeof(TMessage), ResultType: typeof(TResult), RoutingKey: options.RoutingKey);
        var bindings = _handlerBindings.GetValueOrDefault(route) ?? Enumerable.Empty<HandlerBind>();

        using var serviceScope = _serviceScopeFactory.CreateScope();
        var serviceProvider = serviceScope.ServiceProvider;
        var handlers = bindings
            .Select(t => t.Handler ?? serviceProvider.GetRequiredService(t.HandlerType!))
            .Cast<IHandler<TMessage, TResult>>();
        return await handlers.Single().HandleAsync(message, options, token);
    }
}