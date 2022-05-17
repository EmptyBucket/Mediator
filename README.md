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

// configure rabbitMq and redis
serviceCollection
    .RegisterEasyNetQ("host=localhost")
    .AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost"));

// configure mediator (configuration can also be done dynamically on IMediator)
serviceCollection
    .AddMediator(b =>
    {
        // pipe bindings are needed in order not to have an explicit dependency on libs
        // bindings register a type to itself and all its pipe interfaces
        b.BindRabbitMq().BindRedisMq();
    }, async (p, c) =>
    {
        var pipeFactory = p.GetRequiredService<IPipeFactory>();

        // bindings usage
        var rabbitMqPipe = pipeFactory.Create<IConnectingPipe>("RabbitMqPipe");
        var redisMqPipe = pipeFactory.Create<IConnectingPipe>("RedisMqPipe");
        // notice stream support only pubsub, so its use IConnectingPubPipe
        var redisStreamPipe = pipeFactory.Create<IConnectingPubPipe>("RedisStreamPipe");

        // mediator =[Event]> EventHandler
        await c.ConnectHandlerAsync(new EventHandler());

        // mediator =[Event]> rabbitMq =[Event]> EventHandler#1
        // mediator =[Event]> rabbitMq =[Event]> EventHandler#2
        // specify subscriptionId for persistent queues/streams, when has several consumers
        await rabbitMqPipe.ConnectInAsync<Event>(c);
        await rabbitMqPipe.ConnectHandlerAsync(new EventHandler(), subscriptionId: "1");
        await rabbitMqPipe.ConnectHandlerAsync(new EventHandler(), subscriptionId: "2");

        // mediator =[Event]> redisMq =[Event]> EventHandler
        await redisMqPipe.ConnectInAsync<Event>(c);
        await redisMqPipe.ConnectHandlerAsync(new EventHandler());
        
        // mediator =[Event]> redisStream =[Event]> EventHandler
        await redisStreamPipe.ConnectInAsync<Event>(c);
        await redisStreamPipe.ConnectHandlerAsync(new EventHandler(), subscriptionId: "1");

        // mediator =[Event]> rabbitMq =[Event]> redisMq =[Event]> EventHandler =[EventResult]> result
        // you can connect any pipes with each other, building the necessary topology
        await rabbitMqPipe.ConnectInAsync<Event, EventResult>(c);
        await redisMqPipe.ConnectInAsync<Event, EventResult>(rabbitMqPipe);
        await redisMqPipe.ConnectHandlerAsync(new EventHandlerWithResult());
    });
var serviceProvider = serviceCollection.BuildServiceProvider();
var mediator = await serviceProvider.GetRequiredService<IMediatorFactory>().CreateAsync();

// publish and send events
await mediator.PublishAsync(new Event());
var result = await mediator.SendAsync<Event, EventResult>(new Event());
```
