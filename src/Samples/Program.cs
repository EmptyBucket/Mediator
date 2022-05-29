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
using Void = Mediator.Handlers.Void;

var serviceCollection = new ServiceCollection();

// configure RabbitMq and Redis
serviceCollection
    .RegisterEasyNetQ("host=localhost")
    .AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost"));

// configure mediator factory
serviceCollection
    .AddMediatorFactory(bind =>
    {
        // pipe bindings are needed in order not to have an explicit dependency on libs
        // bindings register a type to itself and all its pipe interfaces
        bind.BindRabbitMq().BindRedis();
    }, async (serviceProvider, mediatorTopology) =>
    {
        var (dispatchPipe, receivePipe) = mediatorTopology;

        // bindings usage
        var pipeFactory = serviceProvider.GetRequiredService<IPipeFactory>();
        var connectingPipe = pipeFactory.Create<Pipe>();
        // you must specify name when use interface
        var rabbitMqPipe = pipeFactory.Create<IConnectingPipe>("RabbitMqPipe");
        var redisMqPipe = pipeFactory.Create<IConnectingPipe>("RedisMqPipe");
        // notice stream support only pubsub, so its use IConnectingPubPipe
        var redisStreamPipe = pipeFactory.Create<IConnectingPubPipe>("RedisStreamPipe");

        // mediator =[Event]> EventHandler
        await dispatchPipe.ConnectHandlerAsync(new EventHandler());

        // mediator =[Event]> rabbitMqPipe =[Event]> EventHandler#1
        // mediator =[Event]> rabbitMqPipe =[Event]> EventHandler#2
        // you must specify subscriptionId for persistent queues/streams, when has several consumers
        await dispatchPipe.ConnectOutAsync<Event>(rabbitMqPipe);
        await rabbitMqPipe.ConnectHandlerAsync(new EventHandler(), subscriptionId: "1");
        await rabbitMqPipe.ConnectHandlerAsync(new EventHandler(), subscriptionId: "2");

        // mediator =[Event]> redisMqPipe =[Event]> redisStream =[Event]> EventHandler
        // you can connect any pipes with each other, building the necessary topology
        await dispatchPipe.ConnectOutAsync<Event>(redisMqPipe);
        await redisMqPipe.ConnectOutAsync<Event>(redisStreamPipe);
        await redisStreamPipe.ConnectHandlerAsync(new EventHandler());

        // mediator =[Event]> redisMqPipe =[Event]> EventHandlerWithResult =[EventResult]> result
        // you can wait for result
        await dispatchPipe.ConnectOutAsync<Event, EventResult>(redisMqPipe);
        await redisMqPipe.ConnectHandlerAsync(new EventHandlerWithResult());

        // mediator =[Event]> redisMqPipe =[Event]> mediator =[Event]> EventHandlerWithVoid =[Void]>
        // you can wait for Void
        await dispatchPipe.ConnectOutAsync<Event, Void>(redisMqPipe);
        // you can use receivePipe for a single point configuration receive topology
        await receivePipe.ConnectInAsync<Event, Void>(redisMqPipe);
        await receivePipe.ConnectHandlerAsync(new EventHandlerWithVoid());
    });

// you can also skip configuration that was above and configure IMediator on the fly (use IMediator.Topology for it)
serviceCollection.AddMediator();

var serviceProvider = serviceCollection.BuildServiceProvider();
var mediator = await serviceProvider.GetRequiredService<IMediatorFactory>().CreateAsync();

// publish and send events
// Mediator, MediatorFactory, MediatorTopology, Pipes are thread safe
await mediator.PublishAsync(new Event());
var result = await mediator.SendAsync<Event, EventResult>(new Event());
await mediator.SendAsync<Event, Void>(new Event());
await Task.Delay(TimeSpan.FromHours(1));