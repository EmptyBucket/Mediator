using FlexMediator.Pipes;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediator;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Action<IPipeFactory, IPipeConnector>? make = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton) =>
        AddMediator(serviceCollection, (_, f, p) => make?.Invoke(f, p), lifetime);

    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Action<IServiceProvider, IPipeFactory, IPipeConnector>? make = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        serviceCollection.AddTransient<Pipe>();
        serviceCollection.AddTransient<HandlingPipe>();
        serviceCollection.AddTransient<RabbitMqPipe>();

        serviceCollection.AddSingleton<IPipeFactory, PipeFactory>();

        serviceCollection.Add(new ServiceDescriptor(typeof(IMediator), p =>
        {
            var pipeFactory = p.GetRequiredService<IPipeFactory>();
            var pipe = pipeFactory.Create<Pipe>();
            make?.Invoke(p, pipeFactory, pipe);

            var mediator = new Mediator(pipe);
            return mediator;
        }, lifetime));

        return serviceCollection;
    }
}