// See https://aka.ms/new-console-template for more information

using ConsoleApp5;
using Microsoft.Extensions.DependencyInjection;

var serviceCollection = new ServiceCollection();
serviceCollection.RegisterEasyNetQ("host=localhost")
    .AddMediator(async c =>
    {
        await c.Topologies["direct"].BindDispatch<Event>();
        await c.Topologies["direct"].BindDispatch<Event, string>();
        await c.Topologies["direct"].BindReceive<Event, EventHandler>();
        await c.Topologies["direct"].BindReceive<Event>(new EventHandlerResult());

        await c.Topologies["rabbitmq"].BindDispatch<RabbitMqEvent>();
        await c.Topologies["rabbitmq"].BindDispatch<RabbitMqEvent, string>();
        await c.Topologies["rabbitmq"].BindReceive<RabbitMqEvent, RabbitMqEventHandler>();
        await c.Topologies["rabbitmq"].BindReceive<RabbitMqEvent>(new RabbitMqEventHandlerResult());
    })
    .AddScoped<EventHandler>()
    .AddScoped<RabbitMqEventHandler>();
var serviceProvider = serviceCollection.BuildServiceProvider();

var mediator = serviceProvider.GetRequiredService<IMediator>();
await mediator.Publish(new Event("qwe"));
await mediator.Publish(new RabbitMqEvent("qwe"));
var eventResult = await mediator.Send<Event, string>(new Event("qwe"));
var rabbitMqEventResult = await mediator.Send<RabbitMqEvent, string>(new RabbitMqEvent("qwe"));

await Task.Delay(10_000);

public record Event(string Name);

public record RabbitMqEvent(string Name);

public class EventHandler : IHandler<Event>
{
    public Task HandleAsync(Event message, MessageOptions options, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}

public class RabbitMqEventHandler : IHandler<RabbitMqEvent>
{
    public Task HandleAsync(RabbitMqEvent message, MessageOptions options, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}

public class EventHandlerResult : IHandler<Event, string>
{
    public Task<string> HandleAsync(Event message, MessageOptions options, CancellationToken token)
    {
        return Task.FromResult(message.ToString());
    }
}

public class RabbitMqEventHandlerResult : IHandler<RabbitMqEvent, string>
{
    public Task<string> HandleAsync(RabbitMqEvent message, MessageOptions options, CancellationToken token)
    {
        return Task.FromResult(message.ToString());
    }
}