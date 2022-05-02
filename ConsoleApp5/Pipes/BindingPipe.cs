using ConsoleApp5.Bindings;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp5.Pipes;

public class BindingPipe : IPipe
{
    private readonly IBindingProvider _bindingProvider;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public BindingPipe(IBindingProvider bindingProvider, IServiceScopeFactory serviceScopeFactory)
    {
        _bindingProvider = bindingProvider;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options, CancellationToken token)
    {
        var bindings = _bindingProvider.GetBindings<TMessage>(options.RoutingKey);

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
        var bindings = _bindingProvider.GetBindings<TMessage>(options.RoutingKey);

        using var serviceScope = _serviceScopeFactory.CreateScope();
        var serviceProvider = serviceScope.ServiceProvider;
        var handlers = bindings
            .Select(t => t.Handler ?? serviceProvider.GetRequiredService(t.HandlerType!))
            .Cast<IHandler<TMessage, TResult>>();
        return await handlers.Single().HandleAsync(message, options, token);
    }
}