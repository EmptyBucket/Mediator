using ConsoleApp5.Pipes;
using ConsoleApp5.Registries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ConsoleApp5;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Action<ITopologyRegistry>? topologyRegistryBuilder = null,
        Action<ITransportRegistry>? transportRegistryBuilder = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        var serviceDescriptor = new ServiceDescriptor(typeof(IMediator), p =>
        {
            var transportRegistry = new TransportRegistry(p.GetRequiredService);
            transportRegistryBuilder?.Invoke(transportRegistry);

            var topologyRegistry = new TopologyRegistry(p.GetRequiredService, transportRegistry);
            topologyRegistryBuilder?.Invoke(topologyRegistry);

            transportRegistry.AddTransport<HandlingPipe>("default");

            var forkingPipe = new ForkingPipe(topologyRegistry);
            var logger = p.GetRequiredService<ILogger<LoggingPipe>>();
            var loggingPipe = new LoggingPipe(forkingPipe, logger);
            var mediator = new Mediator(loggingPipe, topologyRegistry);
            return mediator;
        }, lifetime);
        serviceCollection.Add(serviceDescriptor);

        return serviceCollection;
    }
}