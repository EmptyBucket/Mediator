using ConsoleApp5;
using FlexMediator;
using FlexMediator.Pipes;
using FlexMediator.Utils;
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
await rabbitMqPipe.From<Event, EventResult>(mediator);

var redisMqPipe = pipeFactory.Create<RedisMqPipe>();
await redisMqPipe.From<Event, EventResult>(rabbitMqPipe);

//todo потокобезопасность
//todo попробовать сделать так, чтобы при падении одного сообщения не запускались все хендлеры на ретрае
var handlingPipe = pipeFactory.Create<HandlingPipe>();
await handlingPipe.From<Event, EventResult>(redisMqPipe);
handlingPipe.BindHandler<Event, EventResult>(_ => new EventHandler());

await mediator.Publish(new Event("qwe"));
var result = await mediator.Send<Event, EventResult>(new Event("qwe"));

await Task.Delay(10_000);