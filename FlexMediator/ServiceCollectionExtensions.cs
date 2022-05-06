using FlexMediator.Pipes;
using FlexMediator.Plumbers;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediator;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Action<IServiceProvider, IPipeConnector>? make = null, ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        serviceCollection.AddTransient<Pipe>();
        serviceCollection.AddTransient<HandlingPipe>();
        serviceCollection.AddTransient<RabbitMqPipe>();
        
        serviceCollection.AddSingleton<IPipeFactory, PipeFactory>();
        
        serviceCollection.AddTransient<IPlumber, Plumber>();

        serviceCollection.Add(new ServiceDescriptor(typeof(IMediator), p =>
        {
            var plumber = p.GetRequiredService<Plumber>();
            make?.Invoke(p, plumber);
            var mediator = new Mediator(pipe);
            return mediator;
        }, lifetime));

        return serviceCollection;
    }
}