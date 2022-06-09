# Mediator
Transparent mediator, allows messages to be routed according to the configured topology. Can integrate with ```RabbitMq```, ```RedisMq```, ```RedisStream```. Supports ```pub/sub```, ```request/response``` models and dynamic configuration
#### Nuget:
* https://www.nuget.org/packages/ap.Mediator/
* https://www.nuget.org/packages/ap.Mediator.RabbitMq/
* https://www.nuget.org/packages/ap.Mediator.Redis/
## Usage
An example of how a pipelines can be configured:
```csharp
var serviceCollection = new ServiceCollection();

// configure RabbitMq and Redis
serviceCollection
    .RegisterEasyNetQ("host=localhost")
    .AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost"));

// configure mediator
serviceCollection
    .AddMediator(bind =>
    {
        // pipe bindings are needed in order not to have an explicit dependency on libs
        // bindings register a type to itself and all its pipe interfaces
        bind.BindRabbitMq().BindRedis();
    }, (serviceProvider, mediatorTopology) =>
    {
        var (dispatchPipe, receivePipe, pipeFactory) = mediatorTopology;

        // bindings usage
        var pipe = pipeFactory.Create<Pipe>();
        // you must specify name when use interface
        var rabbitMqPipe = pipeFactory.Create<IConnectingPipe>("RabbitMqPipe");
        var redisMqPipe = pipeFactory.Create<IConnectingPipe>("RedisMqPipe");
        // notice stream support only pubsub, so its use IConnectingPubPipe
        var redisStreamPipe = pipeFactory.Create<IConnectingPubPipe>("RedisStreamPipe");

        // mediator =[Event]> EventHandler
        dispatchPipe.ConnectHandler(new EventHandler());

        // mediator =[Event]> rabbitMqPipe =[Event]> EventHandler#1
        // mediator =[Event]> rabbitMqPipe =[Event]> EventHandler#2
        // you must specify subscriptionId for persistent queues/streams, when has several consumers
        // if you want multiple consumers in a consumer group:
        // in rabbitmq use the same subscriptionId for several consumers
        // in redis use delimiter ':' (i.e. subscriptionId = "groupName:consumerName")
        dispatchPipe.ConnectOut<Event>(rabbitMqPipe);
        rabbitMqPipe.ConnectHandler(new EventHandler(), subscriptionId: "1");
        rabbitMqPipe.ConnectHandler(new EventHandler(), subscriptionId: "2");

        // mediator =[Event]> redisMqPipe =[Event]> redisStream =[Event]> EventHandler
        // you can connect any pipes with each other, building the necessary topology
        dispatchPipe.ConnectOut<Event>(redisMqPipe);
        redisMqPipe.ConnectOut<Event>(redisStreamPipe);
        redisStreamPipe.ConnectHandler(new EventHandler());

        // mediator =[Event]> redisMqPipe =[Event]> EventHandlerWithResult =[EventResult]> result
        // you can wait for result
        dispatchPipe.ConnectOut<Event, EventResult>(redisMqPipe);
        redisMqPipe.ConnectHandler(new EventHandlerWithResult());

        // mediator =[Event]> redisMqPipe =[Event]> mediator =[Event]> EventHandlerWithVoid =[Void]>
        // you can wait for Void
        dispatchPipe.ConnectOut<Event, Void>(redisMqPipe);
        // you can use receivePipe for a single point configuration receive topology
        receivePipe.ConnectIn<Event, Void>(redisMqPipe);
        receivePipe.ConnectHandler(new EventHandlerWithVoid());
        // to disconnect any Pipe use Dispose[Async]
    });

var serviceProvider = serviceCollection.BuildServiceProvider();
var mediator = serviceProvider.GetRequiredService<IMediator>();

// you can also skip configuration that was above and configure IMediator on the fly
var (dispatchPipe, receivePipe, pipeFactory) = mediator.Topology;
var rabbitMqPipe = pipeFactory.Create<IConnectingPipe>("RabbitMqPipe");
await dispatchPipe.ConnectOutAsync<Event, EventAnotherResult>(rabbitMqPipe);
await rabbitMqPipe.ConnectHandlerAsync(new EventHandlerWithAnotherResult());

// publish and send events
// Mediator, MediatorTopology, Pipes are thread safe
await mediator.PublishAsync(new Event());
var result = await mediator.SendAsync<Event, EventResult>(new Event());
var anotherResult = await mediator.SendAsync<Event, EventAnotherResult>(new Event());
await mediator.SendAsync<Event, Void>(new Event());
```
