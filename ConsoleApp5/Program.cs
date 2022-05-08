using ConsoleApp5;
using FlexMediator;
using FlexMediator.Pipes;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using EventHandler = ConsoleApp5.EventHandler;

var serviceCollection = new ServiceCollection();
serviceCollection
    .RegisterEasyNetQ("host=localhost")
    .AddMediator(async (f, p) =>
    {
        var connectionMultiplexer = ConnectionMultiplexer.Connect("localhost");
        var subscriber = connectionMultiplexer.GetSubscriber();
        
        var redisMqPipe = new RedisMqPipe(subscriber);
        await redisMqPipe.In<Event>(p);
        await redisMqPipe.In<Event, string>(p);

        var handlingPipe = f.Create<HandlingPipe>();
        await handlingPipe.In<Event>(redisMqPipe);
        await handlingPipe.In<Event, string>(redisMqPipe);

        handlingPipe.Out<Event>(_ => new EventHandler());
        handlingPipe.Out<Event, string>(_ => new EventHandler());
    });
var serviceProvider = serviceCollection.BuildServiceProvider();

var mediator = serviceProvider.GetRequiredService<IMediator>();
// await mediator.Publish(new Event("qwe"));
var result = await mediator.Send<Event, string>(new Event("qwe"));

await Task.Delay(10_000);