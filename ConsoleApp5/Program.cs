using ConsoleApp5;
using FlexMediator;
using FlexMediator.Pipes;
using Microsoft.Extensions.DependencyInjection;
using EventHandler = ConsoleApp5.EventHandler;

var serviceCollection = new ServiceCollection();
serviceCollection.RegisterEasyNetQ("host=localhost")
    .AddMediator(async (sp, p) =>
    {
        var rabbitMqPipe = sp.GetRequiredService<RabbitMqPipe>();
        await p.Connect<Event, RabbitMqPipe>(rabbitMqPipe);

        var handlingPipe = sp.GetRequiredService<HandlingPipe>();
        await rabbitMqPipe.Connect<Event>(handlingPipe);

        handlingPipe.Connect<Event>(f => sp.GetRequiredService<EventHandler>());
    })
    .AddScoped<EventHandler>();
var serviceProvider = serviceCollection.BuildServiceProvider();

var mediator = serviceProvider.GetRequiredService<IMediator>();
await mediator.Publish(new Event("qwe"));
var eventResult = await mediator.Send<Event, string>(new Event("qwe"));

await Task.Delay(10_000);