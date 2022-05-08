using FlexMediator.Pipes;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediator;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Action<IPipeFactory, IPipeConnector>? builder = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton) =>
        AddMediator(serviceCollection, (_, f, p) => builder?.Invoke(f, p), lifetime);

    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Action<IServiceProvider, IPipeFactory, IPipeConnector>? builder = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        serviceCollection.AddTransient<Pipe>();
        serviceCollection.AddTransient<HandlingPipe>();
        serviceCollection.AddTransient<RabbitMqPipe>();
        serviceCollection.AddTransient<RedisMqPipe>();

        serviceCollection.AddSingleton<IPipeFactory, PipeFactory>();

        serviceCollection.Add(new ServiceDescriptor(typeof(IMediator), p =>
        {
            var mediator = new Mediator();
            
            var pipeFactory = p.GetRequiredService<IPipeFactory>();
            builder?.Invoke(p, pipeFactory, mediator.PipeConnector);

            return mediator;
        }, lifetime));

        return serviceCollection;
    }
}