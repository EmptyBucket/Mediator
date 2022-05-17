using Mediator;
using Mediator.Configurations;
using Mediator.Pipes;
using Mediator.Pipes.PublishSubscribe;
using Mediator.RabbitMq.Configurations;
using Mediator.Redis.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Samples.Events;
using Samples.Handlers;
using StackExchange.Redis;
using EventHandler = Samples.Handlers.EventHandler;

var serviceCollection = new ServiceCollection();

// configure RabbitMq and Redis
serviceCollection
    .RegisterEasyNetQ("host=localhost")
    .AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost"));

// configure mediator (configuration can also be done dynamically on IMediator)
serviceCollection
    .AddMediatorFactory(bind =>
    {
        // pipe bindings are needed in order not to have an explicit dependency on libs
        // bindings register a type to itself and all its pipe interfaces
        bind.BindRabbitMq().BindRedisMq();
    }, async (serviceProvider, mediator) =>
    {
        var (dispatch, receive) = mediator.Topology;
        
        // bindings usage
        var pipeFactory = serviceProvider.GetRequiredService<IPipeFactory>();
        var connectingPipe = pipeFactory.Create<ConnectingPipe>();
        // when you use an interface you must also provide a name
        var rabbitMqPipe = pipeFactory.Create<IConnectingPipe>("RabbitMqPipe");
        var redisMqPipe = pipeFactory.Create<IConnectingPipe>("RedisMqPipe");
        // notice stream support only pubsub, so its use IConnectingPubPipe
        var redisStreamPipe = pipeFactory.Create<IConnectingPubPipe>("RedisStreamPipe");

        // mediator =[Event]> EventHandler
        await dispatch.ConnectHandlerAsync(new EventHandler());

        // mediator =[Event]> RabbitMqPipe =[Event]> EventHandler#1
        // mediator =[Event]> RabbitMqPipe =[Event]> EventHandler#2
        await dispatch.ConnectOutAsync<Event>(rabbitMqPipe);
        await rabbitMqPipe.ConnectHandlerAsync(new EventHandler(), subscriptionId: "1");
        await rabbitMqPipe.ConnectHandlerAsync(new EventHandler(), subscriptionId: "2");

        // mediator =[Event]> RedisMqPipe =[Event]> EventHandler
        await redisMqPipe.ConnectInAsync<Event>(dispatch);
        await redisMqPipe.ConnectHandlerAsync(new EventHandler());
        
        // mediator =[Event]> RedisStreamPipe =[Event]> EventHandler
        // specify subscriptionId for persistent queues/streams
        await redisStreamPipe.ConnectInAsync<Event>(dispatch);
        await redisStreamPipe.ConnectHandlerAsync(new EventHandler(), subscriptionId: "1");

        // mediator =[Event]> RabbitMqPipe =[Event]> RedisMqPipe =[Event]> EventHandler =[EventResult]> result
        // you can connect any pipes with each other, building the necessary topology
        await rabbitMqPipe.ConnectInAsync<Event, EventResult>(dispatch);
        await redisMqPipe.ConnectInAsync<Event, EventResult>(rabbitMqPipe);
        await redisMqPipe.ConnectHandlerAsync(new EventHandlerWithResult());

        await dispatch.ConnectOutAsync<Event>(connectingPipe);
        await receive.ConnectInAsync<Event>(connectingPipe);
        await receive.ConnectHandlerAsync(new EventHandler());
    });
var serviceProvider = serviceCollection.BuildServiceProvider();
var mediator = await serviceProvider.GetRequiredService<IMediatorFactory>().CreateAsync();

// publish and send events
await mediator.PublishAsync(new Event());
var result = await mediator.SendAsync<Event, EventResult>(new Event());
await Task.Delay(TimeSpan.FromHours(1));