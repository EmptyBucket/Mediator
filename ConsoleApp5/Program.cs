using ConsoleApp5;
using FlexMediator;
using FlexMediator.Pipes;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using EventHandler = ConsoleApp5.EventHandler;

var serviceProvider = new ServiceCollection()
    .RegisterEasyNetQ("host=localhost")
    .AddSingleton<ISubscriber>(_ => ConnectionMultiplexer.Connect("localhost").GetSubscriber())
    .AddMediator()
    .BuildServiceProvider();

var mediator = serviceProvider.GetRequiredService<IMediator>();
var pipeFactory = serviceProvider.GetRequiredService<IPipeFactory>();

var rabbitMqPipe = pipeFactory.Create<RabbitMqPipe>();
await rabbitMqPipe.In<Event, EventResult>(mediator.PipeConnector);

var redisMqPipe = pipeFactory.Create<RedisMqPipe>();
await redisMqPipe.In<Event, EventResult>(rabbitMqPipe);

//todo попробовать сделать так, чтобы при падении одного сообщения не запускались все хендлеры на ретрае
var handlingPipe = pipeFactory.Create<HandlingPipe>();
await handlingPipe.In<Event, EventResult>(redisMqPipe);
handlingPipe.BindHandler<Event, EventResult>(_ => new EventHandler());

await mediator.Publish(new Event("qwe"));
var result = await mediator.Send<Event, EventResult>(new Event("qwe"));

await Task.Delay(10_000);