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
        AddPipeFactory(serviceCollection, bindPipes);

        serviceCollection.TryAdd(new ServiceDescriptor(typeof(IMediator),
            p => new MediatorFactory(p).CreateAsync().Result, lifetime));

        return serviceCollection;
    }

    public static IServiceCollection AddMediatorFactory(this IServiceCollection serviceCollection,
        Action<IPipeBinder>? bindPipes = null, Func<IServiceProvider, MediatorTopology, Task>? connectPipes = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        AddPipeFactory(serviceCollection, bindPipes);

        serviceCollection.TryAdd(new ServiceDescriptor(typeof(IMediatorFactory),
            p => new MediatorFactory(p, connectPipes), lifetime));

        return serviceCollection;
    }

    private static void AddPipeFactory(IServiceCollection serviceCollection, Action<IPipeBinder>? bindPipes = null)
    {
        bindPipes ??= _ => { };
        var pipeBinder = new PipeBinder();
        BindDefaultPipes(pipeBinder);
        bindPipes(pipeBinder);
        var pipeBinds = pipeBinder.Build();
        serviceCollection.TryAddScoped<IPipeFactory>(p => new PipeFactory(pipeBinds, p));
    }

    private static void BindDefaultPipes(IPipeBinder pipeBinder) =>
        pipeBinder
            .Bind(typeof(HandlingPipe<>))
            .BindInterfaces(typeof(HandlingPipe<>), "HandlingPipe<>")
            .Bind(typeof(HandlingPipe<,>))
            .BindInterfaces(typeof(HandlingPipe<,>), "HandlingPipe<,>")
            .Bind<Pipe>()
            .BindInterfaces<Pipe>(nameof(Pipe));
}