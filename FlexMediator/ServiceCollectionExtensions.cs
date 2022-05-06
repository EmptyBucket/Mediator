using FlexMediator.Pipes;
using FlexMediator.Plumbers;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediator;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Action<IServiceProvider, IPlumber>? make = null, ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        serviceCollection.AddTransient<Pipe>();
        serviceCollection.AddTransient<HandlingPipe>();
        serviceCollection.AddTransient<RabbitMqPipe>();
        
        serviceCollection.AddSingleton<IPipeFactory, PipeFactory>();
        
        serviceCollection.Add(new ServiceDescriptor(typeof(IMediator), p =>
        {
            var pipe = new Pipe();
            
            var plumber = new Plumber(pipe, p.GetRequiredService<IPipeFactory>());
            make?.Invoke(p, plumber);
            
            var mediator = new Mediator(pipe);
            return mediator;
        }, lifetime));

        return serviceCollection;
    }
}