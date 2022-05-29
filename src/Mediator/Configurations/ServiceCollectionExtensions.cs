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
        TryAddPipeFactory(serviceCollection, bindPipes, lifetime);

        serviceCollection.TryAdd(new ServiceDescriptor(typeof(IMediator),
            p => new MediatorFactory(p, (_, _) => Task.CompletedTask).CreateAsync().Result, lifetime));

        return serviceCollection;
    }

    public static IServiceCollection AddMediatorFactory(this IServiceCollection serviceCollection,
        Action<IPipeBinder>? bindPipes = null, Func<IServiceProvider, MediatorTopology, Task>? connectPipes = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        TryAddPipeFactory(serviceCollection, bindPipes, lifetime);

        if (connectPipes is not null) serviceCollection.AddSingleton(new ConnectPipes(connectPipes));

        serviceCollection.TryAdd(new ServiceDescriptor(typeof(IMediatorFactory), p =>
        {
            var connect = p
                .GetServices<ConnectPipes>()
                .Aggregate(new ConnectPipes((_, _) => Task.CompletedTask), (a, n) => async (sp, t) =>
                {
                    await a(sp, t);
                    await n(sp, t);
                });
            return new MediatorFactory(p, new Func<IServiceProvider, MediatorTopology, Task>(connect));
        }, lifetime));

        return serviceCollection;
    }

    private static void TryAddPipeFactory(IServiceCollection serviceCollection, Action<IPipeBinder>? bindPipes = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        static void BindDefaultPipes(IPipeBinder pipeBinder) =>
            pipeBinder
                .Bind(typeof(HandlingPipe<>))
                .BindInterfaces(typeof(HandlingPipe<>), "HandlingPipe<>")
                .Bind(typeof(HandlingPipe<,>))
                .BindInterfaces(typeof(HandlingPipe<,>), "HandlingPipe<,>")
                .Bind<Pipe>()
                .BindInterfaces<Pipe>(nameof(Pipe));

        if (bindPipes is not null) serviceCollection.AddSingleton(new BindPipes(bindPipes));

        serviceCollection.TryAddSingleton(p =>
        {
            var bind = p.GetServices<BindPipes>().Aggregate(new BindPipes(BindDefaultPipes), (a, n) => a + n);
            var pipeBinder = new PipeBinder();
            bind(pipeBinder);
            return pipeBinder;
        });
        serviceCollection.TryAdd(new ServiceDescriptor(typeof(IPipeFactory),
            p => new PipeFactory(p.GetRequiredService<PipeBinder>().Build(), p), lifetime));
    }

    private delegate void BindPipes(IPipeBinder pipeBinder);

    private delegate Task ConnectPipes(IServiceProvider serviceProvider, MediatorTopology mediatorTopology);
}