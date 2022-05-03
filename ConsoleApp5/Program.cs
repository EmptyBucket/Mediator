// See https://aka.ms/new-console-template for more information

using ConsoleApp5;
using FlexMediator;
using Microsoft.Extensions.DependencyInjection;
using EventHandler = ConsoleApp5.EventHandler;

var serviceCollection = new ServiceCollection();
serviceCollection.RegisterEasyNetQ("host=localhost")
    .AddMediator(async c =>
    {
        await c.Topologies["direct"].BindDispatch<Event>();
        await c.Topologies["direct"].BindReceive<Event, EventHandler>();

        await c.Topologies["direct"].BindDispatch<Event, string>();
        await c.Topologies["direct"].BindReceive(new EventHandlerResult());

        await c.Topologies["rabbitmq"].BindDispatch<RabbitMqEvent>();
        await c.Topologies["rabbitmq"].BindReceive<RabbitMqEvent, RabbitMqEventHandler>();

        await c.Topologies["rabbitmq"].BindDispatch<RabbitMqEvent, string>();
        await c.Topologies["rabbitmq"].BindReceive(new RabbitMqEventHandlerResult());
    })
    .AddScoped<EventHandler>()
    .AddScoped<RabbitMqEventHandler>();
var serviceProvider = serviceCollection.BuildServiceProvider();

var mediator = serviceProvider.GetRequiredService<IMediator>();
await mediator.Publish(new Event("qwe"));
var eventResult = await mediator.Send<Event, string>(new Event("qwe"));

await mediator.Publish(new RabbitMqEvent("qwe"));
var rabbitMqEventResult = await mediator.Send<RabbitMqEvent, string>(new RabbitMqEvent("qwe"));

await Task.Delay(10_000);