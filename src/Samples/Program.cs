using Mediator;
using Mediator.Configurations;
using Mediator.Pipes;
using Mediator.RabbitMq.Configurations;
using Mediator.Redis.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Samples.Events;
using Samples.Handlers;
using StackExchange.Redis;
using EventHandler = Samples.Handlers.EventHandler;

var serviceCollection = new ServiceCollection();
serviceCollection
    .RegisterEasyNetQ("host=localhost")
    .AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost"));
serviceCollection
    .AddMediator(b => b.BindRabbitMq().BindRedisMq(), async (p, c) =>
    {
        // configuration can also be done dynamically on IMediator
        var pipeFactory = p.GetRequiredService<IPipeFactory>();

        // configure rabbitMq pipeline
        var rabbitMqPipe = pipeFactory.Create<IBranchingPipe>("rabbit");
        await rabbitMqPipe.ConnectInAsync<Event>(c);
        await rabbitMqPipe.ConnectInAsync<Event, EventResult>(c);
        await rabbitMqPipe.ConnectInAsync<AnotherEvent>(c);
        await rabbitMqPipe.ConnectOutAsync(new EventHandler(), subscriptionId: "1");
        await rabbitMqPipe.ConnectOutAsync(new EventHandler(), subscriptionId: "2");

        // configure redisMq pipeline
        var redisMqPipe = pipeFactory.Create<IBranchingPipe>("redis");
        await redisMqPipe.ConnectInAsync<Event, EventResult>(rabbitMqPipe);
        await redisMqPipe.ConnectInAsync<AnotherEvent>(c);
        await redisMqPipe.ConnectOutAsync(new EventHandlerWithResult());
        await redisMqPipe.ConnectOutAsync(new AnotherEventHandler());
    });
var serviceProvider = serviceCollection.BuildServiceProvider();
var mediator = await serviceProvider.GetRequiredService<IMediatorFactory>().CreateAsync();

await mediator.PublishAsync(new Event());
await mediator.PublishAsync(new AnotherEvent());
var result = await mediator.SendAsync<Event, EventResult>(new Event());

await Task.Delay(TimeSpan.FromHours(1));