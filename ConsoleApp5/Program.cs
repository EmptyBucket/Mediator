using ConsoleApp5;
using FlexMediator;
using FlexMediator.Pipes;
using FlexMediator.Pipes.RabbitMq;
using FlexMediator.Pipes.RedisMq;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using EventHandler = ConsoleApp5.EventHandler;

var serviceProvider = new ServiceCollection()
    // register rabbitMq
    .RegisterEasyNetQ("host=localhost")
    // register redisMq
    .AddSingleton<ISubscriber>(_ => ConnectionMultiplexer.Connect("localhost").GetSubscriber())
    .AddMediator(async (f, c) =>
    {
        var rabbitMqPipe = f.Create<RabbitMqPipe>();

        await rabbitMqPipe.ConnectInAsync<Event>(c);
        await rabbitMqPipe.ConnectOutAsync(_ => new EventHandler(), subscriptionId: "1");
        await rabbitMqPipe.ConnectOutAsync(_ => new EventHandler(), subscriptionId: "2");

        await rabbitMqPipe.ConnectInAsync<Event, EventResult>(c);
        
        await rabbitMqPipe.ConnectInAsync<AnotherEvent>(c);


        var redisMqPipe = f.Create<RedisMqPipe>();

        await redisMqPipe.ConnectInAsync<Event, EventResult>(rabbitMqPipe);
        await redisMqPipe.ConnectOutAsync(_ => new EventHandlerWithResult());

        await redisMqPipe.ConnectInAsync<AnotherEvent>(c);
        await redisMqPipe.ConnectOutAsync(_ => new AnotherEventHandler());
    })
    .BuildServiceProvider();
var mediator = await serviceProvider.GetRequiredService<IMediatorFactory>().CreateAsync();


await mediator.PublishAsync(new Event());
var result = await mediator.SendAsync<Event, EventResult>(new Event());
await mediator.PublishAsync(new AnotherEvent("qwe"));

await Task.Delay(TimeSpan.FromHours(1));