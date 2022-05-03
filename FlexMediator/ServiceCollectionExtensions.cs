using FlexMediator.Pipes;
using FlexMediator.Topologies;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediator;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Action<MediatorConfiguration>? mediatorBuilder = null, ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        var serviceDescriptor = new ServiceDescriptor(typeof(IMediator), p =>
        {
            var dispatchPipeBinder = new PipeBinder();
            var dispatchPipe = new ForkPipe(dispatchPipeBinder);
            var directTopology = ActivatorUtilities.CreateInstance<DirectTopologyFactory>(p, dispatchPipeBinder)
                .Create();
            var rabbitMqTopology = ActivatorUtilities.CreateInstance<RabbitMqTopologyFactory>(p, dispatchPipeBinder)
                .Create();
            var topologies = new Dictionary<string, Topology>
            {
                { "direct", directTopology },
                { "rabbitmq", rabbitMqTopology }
            };
            var mediatorConfiguration = new MediatorConfiguration(topologies);
            var mediator = new Mediator(dispatchPipe, mediatorConfiguration);
            mediatorBuilder?.Invoke(mediator.Configuration);
            return mediator;
        }, lifetime);
        serviceCollection.Add(serviceDescriptor);

        return serviceCollection;
    }
}