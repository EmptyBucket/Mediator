using FlexMediator.Pipes;
using FlexMediator.Pipes.RabbitMq;
using FlexMediator.Pipes.RedisMq;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediator;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        ServiceLifetime lifetime = ServiceLifetime.Singleton) =>
        AddMediator(serviceCollection, (_, _, _) => { }, lifetime);

    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Action<IPipeFactory, IPipeConnector> builder,
        ServiceLifetime lifetime = ServiceLifetime.Singleton) =>
        AddMediator(serviceCollection, (_, f, p) => builder(f, p), lifetime);

    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Action<IServiceProvider, IPipeFactory, IPipeConnector> builder,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        serviceCollection.AddTransient(typeof(HandlingPipe<>));
        serviceCollection.AddTransient(typeof(HandlingPipe<,>));
        serviceCollection.AddTransient<Pipe>();
        serviceCollection.AddTransient<RabbitMqPipe>();
        serviceCollection.AddTransient<RedisMqPipe>();

        serviceCollection.AddSingleton<IPipeFactory, PipeFactory>();

        serviceCollection.Add(new ServiceDescriptor(typeof(Mediator), p =>
        {
            var mediator = new Mediator(p);

            var pipeFactory = p.GetRequiredService<IPipeFactory>();
            builder(p, pipeFactory, mediator);

            return mediator;
        }, lifetime));
        serviceCollection.Add(
            new ServiceDescriptor(typeof(IMediator), p => p.GetRequiredService<Mediator>(), lifetime));

        return serviceCollection;
    }
}