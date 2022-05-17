using Mediator.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mediator.Configurations;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Action<IPipeBinder>? pipeBinderBuilder = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        pipeBinderBuilder ??= _ => { };

        var pipeBinder = new PipeBinder();
        pipeBinderBuilder(pipeBinder
            .Bind(typeof(HandlingPipe<>))
            .BindInterfaces(typeof(HandlingPipe<>), "HandlingPipe<>")
            .Bind(typeof(HandlingPipe<,>))
            .BindInterfaces(typeof(HandlingPipe<,>), "HandlingPipe<,>")
            .Bind<ConnectingPipe>()
            .BindInterfaces<ConnectingPipe>(nameof(ConnectingPipe)));
        var pipeBinds = pipeBinder.Build();
        serviceCollection.TryAddScoped<IPipeFactory>(p => new PipeFactory(pipeBinds, p));

        serviceCollection.TryAdd(new ServiceDescriptor(typeof(IMediator), typeof(Mediator), lifetime));

        return serviceCollection;
    }

    public static IServiceCollection AddMediatorFactory(this IServiceCollection serviceCollection,
        Action<IPipeBinder>? pipeBinderBuilder = null,
        Func<IServiceProvider, IPipeConnector, Task>? pipeConnectorBuilder = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        pipeBinderBuilder ??= _ => { };
        pipeConnectorBuilder ??= (_, _) => Task.CompletedTask;

        var pipeBinder = new PipeBinder();
        pipeBinderBuilder(pipeBinder
            .Bind(typeof(HandlingPipe<>))
            .BindInterfaces(typeof(HandlingPipe<>), "HandlingPipe<>")
            .Bind(typeof(HandlingPipe<,>))
            .BindInterfaces(typeof(HandlingPipe<,>), "HandlingPipe<,>")
            .Bind<ConnectingPipe>()
            .BindInterfaces<ConnectingPipe>(nameof(ConnectingPipe)));
        var pipeBinds = pipeBinder.Build();
        serviceCollection.TryAddScoped<IPipeFactory>(p => new PipeFactory(pipeBinds, p));

        serviceCollection.TryAdd(new ServiceDescriptor(
            typeof(IMediatorFactory), p => new MediatorFactory(pipeConnectorBuilder, p), lifetime));

        return serviceCollection;
    }
}