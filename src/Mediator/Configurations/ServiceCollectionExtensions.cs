using Mediator.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mediator.Configurations;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Action<IPipeBinder>? bindPipes = null, Action<IServiceProvider, MediatorTopology>? connectPipes = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        TryAddPipeFactory(serviceCollection, bindPipes, lifetime);

        if (connectPipes is not null) serviceCollection.AddSingleton(new ConnectPipes(connectPipes));

        serviceCollection.TryAdd(new ServiceDescriptor(typeof(IMediator), p =>
        {
            var pipeFactory = p.GetRequiredService<IPipeFactory>();
            var dispatchPipe = pipeFactory.Create<Pipe>();
            var receivePipe = pipeFactory.Create<Pipe>();
            var mediatorTopology = new MediatorTopology(dispatchPipe, receivePipe);
            
            var connect = p.GetServices<ConnectPipes>().Aggregate(new ConnectPipes((_, _) => { }), (a, n) => a + n);
            connect(p, mediatorTopology);
            var mediator = new Mediator(mediatorTopology);
            
            return mediator;
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
            var pipeBinder = new PipeBinder();
            
            var bind = p.GetServices<BindPipes>().Aggregate(new BindPipes(BindDefaultPipes), (a, n) => a + n);
            bind(pipeBinder);
            
            return pipeBinder;
        });
        serviceCollection.TryAdd(new ServiceDescriptor(typeof(IPipeFactory),
            p => new PipeFactory(p.GetRequiredService<PipeBinder>().Build(), p), lifetime));
    }

    private delegate void BindPipes(IPipeBinder pipeBinder);

    private delegate void ConnectPipes(IServiceProvider serviceProvider, MediatorTopology mediatorTopology);
}