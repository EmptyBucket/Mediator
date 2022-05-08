using ConsoleApp5;
using EasyNetQ;
using FlexMediator;
using Microsoft.Extensions.DependencyInjection;
using JsonSerializer = System.Text.Json.JsonSerializer;

var serviceCollection = new ServiceCollection();
serviceCollection
    .RegisterEasyNetQ("host=localhost")
    .AddMediator(async (f, p) =>
    {
    })
    .AddScoped<EventHandler>();
var serviceProvider = serviceCollection.BuildServiceProvider();

var mediator = serviceProvider.GetRequiredService<IMediator>();
// await mediator.Publish(new Event("qwe"));
// var result = await mediator.Send<Event, string>(new Event("qwe"));

var bus = serviceProvider.GetService<IBus>();
var serialize = JsonSerializer.Serialize(new Exception("asdasd"));
await Task.Delay(10_000);