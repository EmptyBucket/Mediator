using FlexMediator.Handlers;
using FlexMediator.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediator.Pipes;

public class HandlingPipe : IPipe, IHandleConnector
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly HandleConnector _handleConnector;

    public HandlingPipe(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _handleConnector = new HandleConnector();
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options,
        CancellationToken token = default)
    {
        var route = new Route(typeof(TMessage), options.RoutingKey);
        var connections = _handleConnector.GetValueOrDefault(route) ?? Enumerable.Empty<HandlerConnection>();

        using var serviceScope = _serviceScopeFactory.CreateScope();
        var serviceProvider = serviceScope.ServiceProvider;
        var handlers = connections.Select(c => c.Factory(serviceProvider)).Cast<IHandler<TMessage>>();
        await Task.WhenAll(handlers.Select(h => h.HandleAsync(message, options, token)));
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token = default)
    {
        var route = new Route(typeof(TMessage), options.RoutingKey, typeof(TResult));
        var connections = _handleConnector.GetValueOrDefault(route) ?? Enumerable.Empty<HandlerConnection>();

        using var serviceScope = _serviceScopeFactory.CreateScope();
        var serviceProvider = serviceScope.ServiceProvider;
        var handlers = connections.Select(c => c.Factory(serviceProvider)).Cast<IHandler<TMessage, TResult>>();
        return await handlers.Single().HandleAsync(message, options, token);
    }

    public HandlerConnection BindHandler<TMessage>(Func<IServiceProvider, IHandler> factory,
        string routingKey = "") =>
        _handleConnector.BindHandler<TMessage>(factory, routingKey);

    public HandlerConnection BindHandler<TMessage, TResult>(Func<IServiceProvider, IHandler> factory,
        string routingKey = "") =>
        _handleConnector.BindHandler<TMessage, TResult>(factory, routingKey);
}