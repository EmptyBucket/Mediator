using ConsoleApp5.Topologies;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp5;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Action<IMediator>? mediatorBuilder = null, ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        var serviceDescriptor = new ServiceDescriptor(typeof(IMediator), p =>
        {
            var topologyRegistry = new DirectTopologyRegistry();
            var pipeRegistry = new TransportRegistry(p);
            var mediator = new Mediator();
            mediator.AddDefaultTransport();
            mediatorBuilder?.Invoke(mediator);
            return mediator;
        }, lifetime);
        serviceCollection.Add(serviceDescriptor);

        return serviceCollection;
    }
}