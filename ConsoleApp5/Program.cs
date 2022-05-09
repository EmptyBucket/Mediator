using ConsoleApp5;
using FlexMediator;
using FlexMediator.Pipes;
using FlexMediator.Pipes.RabbitMq;
using FlexMediator.Pipes.RedisMq;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using EventHandler = ConsoleApp5.EventHandler;

var serviceProvider = new ServiceCollection()
    .RegisterEasyNetQ("host=localhost")
    .AddSingleton<ISubscriber>(_ => ConnectionMultiplexer.Connect("localhost").GetSubscriber())
    .AddMediator()
    .BuildServiceProvider();

var mediator = serviceProvider.GetRequiredService<Mediator>();
var pipeFactory = serviceProvider.GetRequiredService<IPipeFactory>();

var rabbitMqPipe = pipeFactory.Create<RabbitMqPipe>();
await rabbitMqPipe.ConnectInAsync<Event, EventResult>(mediator);

var redisMqPipe = pipeFactory.Create<RedisMqPipe>();
await redisMqPipe.ConnectInAsync<Event, EventResult>(rabbitMqPipe);

await redisMqPipe.ConnectOutAsync(_ => new EventHandler());
await redisMqPipe.ConnectOutAsync(_ => new EventHandler());

await mediator.PublishAsync(new Event("qwe"));
var result = await mediator.SendAsync<Event, EventResult>(new Event("qwe"));

await Task.Delay(TimeSpan.FromHours(1));