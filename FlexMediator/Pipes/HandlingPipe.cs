using FlexMediator.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediator.Pipes;

public class HandlingPipe : IPipe, IHandlerBinder
{
    private readonly HandlerBinder _handlerBinder;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public HandlingPipe(IServiceScopeFactory serviceScopeFactory)
    {
        _handlerBinder = new HandlerBinder();
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options, CancellationToken token)
    {
        var route = new Route(typeof(TMessage), options.RoutingKey);
        var bindings = _handlerBinder.GetValueOrDefault(route) ?? Enumerable.Empty<HandlerBind>();

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
        var route = new Route(typeof(TMessage), options.RoutingKey, typeof(TResult));
        var bindings = _handlerBinder.GetValueOrDefault(route) ?? Enumerable.Empty<HandlerBind>();

        using var serviceScope = _serviceScopeFactory.CreateScope();
        var serviceProvider = serviceScope.ServiceProvider;
        var handlers = bindings
            .Select(t => t.Handler ?? serviceProvider.GetRequiredService(t.HandlerType!))
            .Cast<IHandler<TMessage, TResult>>();
        return await handlers.Single().HandleAsync(message, options, token);
    }

    public HandlerBind Bind<TMessage, THandler>(string routingKey = "") 
        where THandler : IHandler<TMessage>
    {
        return _handlerBinder.Bind<TMessage, THandler>(routingKey);
    }

    public HandlerBind Bind<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>
    {
        return _handlerBinder.Bind<TMessage, TResult, THandler>(routingKey);
    }

    public HandlerBind Bind<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        return _handlerBinder.Bind(handler, routingKey);
    }

    public HandlerBind Bind<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "")
    {
        return _handlerBinder.Bind(handler, routingKey);
    }
}