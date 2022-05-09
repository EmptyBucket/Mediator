using Mediator;
using Mediator.Pipes;
using Mediator.RabbitMq;
using Mediator.RabbitMq.Pipes;
using Mediator.Redis;
using Mediator.Redis.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Samples.Events;
using Samples.Handlers;
using StackExchange.Redis;
using EventHandler = Samples.Handlers.EventHandler;

var serviceProvider = new ServiceCollection()
    // register rabbitMq
    .RegisterEasyNetQ("host=localhost")
    // register redisMq
    .AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost"))
    .AddMediator(async (f, c) =>
    {
        // configure rabbitmq pipeline
        var rabbitMqPipe = f.Create<RabbitMqPipe>();

        await rabbitMqPipe.ConnectInAsync<Event>(c);
        await rabbitMqPipe.ConnectOutAsync(_ => new EventHandler(), subscriptionId: "1");
        await rabbitMqPipe.ConnectOutAsync(_ => new EventHandler(), subscriptionId: "2");

        await rabbitMqPipe.ConnectInAsync<Event, EventResult>(c);
        
        await rabbitMqPipe.ConnectInAsync<AnotherEvent>(c);

        // configure redisMq pipeline
        var redisMqPipe = f.Create<RedisMqPipe>();

        await redisMqPipe.ConnectInAsync<Event, EventResult>(rabbitMqPipe);
        await redisMqPipe.ConnectOutAsync(_ => new EventHandlerWithResult());

        await redisMqPipe.ConnectInAsync<AnotherEvent>(c);
        await redisMqPipe.ConnectOutAsync(_ => new AnotherEventHandler());
    })
    .BuildServiceProvider();
var mediator = await serviceProvider.GetRequiredService<IMediatorFactory>().CreateAsync();


await mediator.PublishAsync(new Event());
await mediator.PublishAsync(new AnotherEvent());
var result = await mediator.SendAsync<Event, EventResult>(new Event());

await Task.Delay(TimeSpan.FromHours(1));