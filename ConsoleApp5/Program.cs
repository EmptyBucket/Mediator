using ConsoleApp5;
using FlexMediator;
using FlexMediator.Pipes;
using Microsoft.Extensions.DependencyInjection;
using EventHandler = ConsoleApp5.EventHandler;

var serviceCollection = new ServiceCollection();
serviceCollection
    .RegisterEasyNetQ("host=localhost")
    .AddMediator(async (f, p) =>
    {
        var prev = f.Create<Pipe>();
        await prev.In<Event>(p);
        var current = f.Create<RabbitMqPipe>();
        var next = f.Create<Pipe>();
    })
    .AddScoped<EventHandler>();
var serviceProvider = serviceCollection.BuildServiceProvider();

var mediator = serviceProvider.GetRequiredService<IMediator>();
await mediator.Publish(new Event("qwe"));
var eventResult = await mediator.Send<Event, string>(new Event("qwe"));

await Task.Delay(10_000);