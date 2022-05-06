using FlexMediator.Pipes;
using FlexMediator.Topologies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlexMediator;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Action<MediatorConfiguration>? mediatorBuilder = null, ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        var serviceDescriptor = new ServiceDescriptor(typeof(IMediator), p =>
        {
            var directTopology = ActivatorUtilities.CreateInstance<DirectTopologyFactory>(p, dispatchPipeBinder)
                .Create();
            var rabbitMqTopology = ActivatorUtilities.CreateInstance<RabbitMqTopologyFactory>(p, dispatchPipeBinder)
                .Create();
            var topologies = new Dictionary<string, RabbitMqTopologyBinder>
            {
                { "direct", directTopology },
                { "rabbitmq", rabbitMqTopology }
            };
            mediatorBuilder?.Invoke(mediator.Configuration);
            return mediator;
        }, lifetime);
        serviceCollection.Add(serviceDescriptor);

        return serviceCollection;
    }
}

public class MediatorBuilder
{
    private readonly ServiceCollection _serviceCollection;
    private readonly Dictionary<string, Func<IServiceProvider, ITopologyBinder>> _topologyBinders = new();

    public MediatorBuilder(ServiceCollection serviceCollection)
    {
        _serviceCollection = new ServiceCollection { serviceCollection };
    }

    public MediatorBuilder AddTopology(string name, Func<IServiceProvider, ITopologyBinder> topologyBinder)
    {
        _topologyBinders[name] = topologyBinder;
        return this;
    }

    public IMediator Build()
    {
        var dispatchPipeBinder = new PipeBinder();
        var dispatchPipe = new BranchingPipe(dispatchPipeBinder);

        var mediatorConfiguration = new MediatorConfiguration();
        var mediator = new Mediator(dispatchPipe, mediatorConfiguration);
        return mediator;
    }
}