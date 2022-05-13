using Mediator.Pipes;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Configurations;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
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
            .Bind<BranchingPipe>()
            .BindInterfaces<BranchingPipe>(nameof(BranchingPipe)));
        var pipeBinds = pipeBinder.Build();
        serviceCollection.AddSingleton<IPipeFactory>(p => new PipeFactory(pipeBinds, p));

        serviceCollection.Add(new ServiceDescriptor(
            typeof(IMediatorFactory), p => new MediatorFactory(pipeConnectorBuilder, p), lifetime));

        return serviceCollection;
    }
}