# Mediator
Transparent mediator, allows messages to be routed according to the configured topology. Can integrate with ```RabbitMq```, ```RedisMq```, ```RedisStream```. Supports ```pub/sub```, ```request/response``` models and dynamic configuration
#### Nuget:
* https://www.nuget.org/packages/ap.Mediator/
* https://www.nuget.org/packages/ap.Mediator.RabbitMq/
* https://www.nuget.org/packages/ap.Mediator.Redis/
## Usage

### Configure RabbitMq and Redis

```csharp
serviceCollection
    .RegisterEasyNetQ("host=localhost")
    .AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost"));
```

### Configure mediator

```csharp
// You can call AddMediator as many times as you like,
// adding to the configuration from different parts of your application
serviceCollection
    .AddMediator(bindPipes: _ => { }, connectPipes: (_, _) => { });
```

### Configure pipe bindings

```csharp
serviceCollection
    .AddMediator(bind =>
    {
        // Pipe binds are needed to simply create and store pipes and transfer them from one part of the application to another.
        // If you request a pipe with the same binding, you will receive the same pipe instance
        bind
            .Bind<RabbitMqPipe>()
            .Bind<RedisMqPipe>()
            .Bind<RedisStreamPipe>();
        // They are also needed in order not to have an explicit dependency on infrastructure libs see #Bindings usage
        bind
            .BindInterfaces<RabbitMqPipe>("RabbitMqPipe")
            .BindInterfaces<RedisMqPipe>("RedisMqPipe")
            .BindInterfaces<RedisStreamPipe>("RedisStreamPipe");
    });
```

### Bindings usage

```csharp
var mediator = serviceProvider0.GetRequiredService<IMediator>();
var (_, _, pipeProvider) = mediator.Topology;

// You can create pipe with explicit type
IPipe rabbitMqPipe = pipeProvider.Get<RabbitMqPipe>();

// You can create pipe with interface, but then you must specify name
rabbitMqPipe = pipeProvider.Get<IPipe>("RabbitMqPipe");
rabbitMqPipe = pipeProvider.Get<IMulticastPipe>("RabbitMqPipe");
// Both will be the same pipe because the same name

// Notice that the stream support only publish/subscribe model so it uses IMulticastPubPipe
var redisStreamPipe = pipeProvider.Get<IMulticastPubPipe>("RedisStreamPipe");
```

### Configure pipe connections

```csharp
serviceCollection
    .AddMediator(b => b
        .BindInterfaces<RabbitMqPipe>(nameof(RabbitMqPipe))
        .BindInterfaces<RedisMqPipe>(nameof(RedisMqPipe))
        .BindInterfaces<RedisStreamPipe>(nameof(RedisStreamPipe)), (_, topology) =>
    {
        var (dispatchPipe, _, pipeProvider) = topology;
        var rabbitMqPipe = pipeProvider.Get<IMulticastPipe>("RabbitMqPipe");
        var redisMqPipe = pipeProvider.Get<IMulticastPipe>("RedisMqPipe");
        var redisStreamPipe = pipeProvider.Get<IMulticastPubPipe>("RedisStreamPipe");

        // You can connect handler or handler factory directly to mediator
        // mediator =[FooEvent]> FooEventHandler
        dispatchPipe.ConnectHandler(new FooEventHandler());

        // You can connect handler or handler factory through transport,
        // e.g rabbitmq or redis
        // You must specify subscriptionId for persistent queues or streams
        // when has several consumers
        // If you want multiple consumers in a consumer group:
        // - in rabbitmq use the same subscriptionId for several consumers
        // - in redis stream use the same groupName
        //   (groupName is separated from the consumerName through the delimiter ':',
        //   i.e. subscriptionId = "{groupName}:{consumerName}")
        // mediator =[FooEvent]> rabbitMqPipe =[FooEvent]> FooEventHandler#1
        // mediator =[FooEvent]> rabbitMqPipe =[FooEvent]> FooEventHandler#2
        dispatchPipe.ConnectOut<FooEvent>(rabbitMqPipe);
        rabbitMqPipe.ConnectHandler(new FooEventHandler(), subscriptionId: "1");
        rabbitMqPipe.ConnectHandler(new FooEventHandler(), subscriptionId: "2");

        // You can connect any pipes with each other, building the necessary topology
        // mediator =[FooEvent]> redisMqPipe =[FooEvent]> redisStream =[FooEvent]> FooEventHandler
        dispatchPipe.ConnectOut<FooEvent>(redisMqPipe);
        redisMqPipe.ConnectOut<FooEvent>(redisStreamPipe);
        redisStreamPipe.ConnectHandler(new FooEventHandler());

        // You can wait for result. To do this, configure the topology with the result type
        // mediator =[Event]> redisMqPipe =[Event]> FooEventHandlerWithFooResult =[FooResult]> result
        dispatchPipe.ConnectOut<FooEvent, FooResult>(redisMqPipe);
        redisMqPipe.ConnectHandler(new FooEventHandlerWithFooResult());

        // You can wait for Void when you don't want the result,
        // but you want to wait in a synchronous manner
        // mediator =[Event]> redisMqPipe =[Event]> mediator =[Event]> FooEventHandlerWithVoid =[Void]>
        dispatchPipe.ConnectOut<FooEvent, Void>(redisMqPipe);
        redisMqPipe.ConnectHandler(new FooEventHandlerWithVoid());
    });
```

### Dynamic configure pipe connections

```csharp
var mediator = serviceProvider1.GetRequiredService<IMediator>();
var (_, _, pipeProvider) = mediator.Topology;
var rabbitMqPipe = pipeProvider.Get<IMulticastPipe>("RabbitMqPipe");

// You can skip pipe connections configuration that was above in AddMediator and
// configure IMediator on the fly
var connection = await rabbitMqPipe.ConnectHandlerAsync(new FooEventHandler(), subscriptionId: "3");

// You can disconnect any Pipe use Dispose[Async]
await connection.DisposeAsync();
```

### Configure routing

```csharp
var mediator = serviceProvider1.GetRequiredService<IMediator>();
var (dispatchPipe, receivePipe, pipeProvider) = mediator.Topology;
var rabbitMqPipe = pipeProvider.Get<IMulticastPipe>("RabbitMqPipe");

// You can specify routingKey for routing in same type
await dispatchPipe.ConnectOutAsync<FooEvent>(rabbitMqPipe, "foo-routing-key");
// You can use receivePipe for a single point configuration receive topology
await rabbitMqPipe.ConnectOutAsync<FooEvent>(receivePipe, "foo-routing-key", subscriptionId: "4");
await receivePipe.ConnectHandlerAsync(new FooEventHandler(), "foo-routing-key");
```

### Publish and send events

Mediator, MediatorTopology, Pipes, PipeConnections are thread safe

```csharp
await mediator.PublishAsync(new FooEvent());
await mediator.PublishAsync(new FooEvent(), new Options { RoutingKey = "foo-routing-key" });
var result = await mediator.SendAsync<FooEvent, FooResult>(new FooEvent());
await mediator.SendAsync<FooEvent, Void>(new FooEvent());
```
