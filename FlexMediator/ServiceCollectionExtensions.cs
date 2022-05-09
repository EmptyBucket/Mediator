using FlexMediator.Pipes;
using FlexMediator.Pipes.RabbitMq;
using FlexMediator.Pipes.RedisMq;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediator;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        ServiceLifetime lifetime = ServiceLifetime.Singleton) =>
        AddMediator(serviceCollection, (IServiceProvider _, IPipeConnector _) => Task.CompletedTask, lifetime);

    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Func<IPipeFactory, IPipeConnector, Task> builder,
        ServiceLifetime lifetime = ServiceLifetime.Singleton) =>
        AddMediator(serviceCollection, (s, p) => builder(s.GetRequiredService<IPipeFactory>(), p), lifetime);

    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Func<IServiceProvider, IPipeConnector, Task> builder,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        serviceCollection.AddTransient(typeof(HandlingPipe<>));
        serviceCollection.AddTransient(typeof(HandlingPipe<,>));
        serviceCollection.AddTransient<Pipe>();
        serviceCollection.AddTransient<RabbitMqPipe>();
        serviceCollection.AddTransient<RedisMqPipe>();

        serviceCollection.AddSingleton<IPipeFactory, PipeFactory>();

        serviceCollection.Add(
            new ServiceDescriptor(typeof(IMediatorFactory), p => new MediatorFactory(builder, p), lifetime));

        return serviceCollection;
    }
}