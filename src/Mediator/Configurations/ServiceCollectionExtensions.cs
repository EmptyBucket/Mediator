using Mediator.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mediator.Configurations;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Action<IPipeBinder>? bindPipes = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        bindPipes ??= _ => { };

        var pipeBinder = new PipeBinder();
        BindDefaults(pipeBinder);
        bindPipes(pipeBinder);
        var pipeBinds = pipeBinder.Build();
        serviceCollection.TryAddScoped<IPipeFactory>(p => new PipeFactory(pipeBinds, p));

        serviceCollection.TryAdd(new ServiceDescriptor(typeof(IMediator), typeof(Mediator), lifetime));

        return serviceCollection;
    }

    public static IServiceCollection AddMediatorFactory(this IServiceCollection serviceCollection,
        Action<IPipeBinder>? bindPipes = null, Func<IServiceProvider, IPipeConnector, Task>? connectPipes = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        bindPipes ??= _ => { };
        connectPipes ??= (_, _) => Task.CompletedTask;

        var pipeBinder = new PipeBinder();
        BindDefaults(pipeBinder);
        bindPipes(pipeBinder);
        var pipeBinds = pipeBinder.Build();
        serviceCollection.TryAddScoped<IPipeFactory>(p => new PipeFactory(pipeBinds, p));

        serviceCollection.TryAdd(
            new ServiceDescriptor(typeof(IMediatorFactory), p => new MediatorFactory(connectPipes, p), lifetime));

        return serviceCollection;
    }

    private static void BindDefaults(IPipeBinder pipeBinder) =>
        pipeBinder
            .Bind(typeof(HandlingPipe<>))
            .BindInterfaces(typeof(HandlingPipe<>), "HandlingPipe<>")
            .Bind(typeof(HandlingPipe<,>))
            .BindInterfaces(typeof(HandlingPipe<,>), "HandlingPipe<,>")
            .Bind<ConnectingPipe>()
            .BindInterfaces<ConnectingPipe>(nameof(ConnectingPipe));
}