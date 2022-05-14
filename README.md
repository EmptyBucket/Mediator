# Mediator
Transparent mediator, allows messages to be routed according to the configured topology. Can integrate with ```rabbitmq``` or ```redismq```. Supports ```pub/sub```, ```request/response``` models and dynamic configuration
### Usage
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
        // pipe bindings are needed in order not to have an explicit dependency on rabbitMq or redis
        b.BindRabbitMq().BindRedisMq();
    }, async (p, c) =>
    {
        var pipeFactory = p.GetRequiredService<IPipeFactory>();

        // bindings usage
        var rabbitMqPipe = pipeFactory.Create<IConnectingPipe>("RabbitMqPipe");
        var redisMqPipe = pipeFactory.Create<IConnectingPipe>("RedisMqPipe");

        // mediator =[Event]> EventHandler
        await c.ConnectOutAsync(new EventHandler());

        // mediator =[Event]> rabbitMq =[Event]> EventHandler#1
        // mediator =[Event]> rabbitMq =[Event]> EventHandler#2
        await rabbitMqPipe.ConnectInAsync<Event>(c);
        // specify subscriptionId for persistent queues
        await rabbitMqPipe.ConnectOutAsync(new EventHandler(), subscriptionId: "1");
        await rabbitMqPipe.ConnectOutAsync(new EventHandler(), subscriptionId: "2");

        // mediator =[AnotherEvent]> redisMq =[AnotherEvent]> AnotherEventHandler
        await redisMqPipe.ConnectInAsync<AnotherEvent>(c);
        await redisMqPipe.ConnectOutAsync(new AnotherEventHandler());

        // mediator =[Event]> rabbitMq =[Event]> redisMq =[Event]> EventHandler#result =[EventResult]> result
        await rabbitMqPipe.ConnectInAsync<Event, EventResult>(c);
        await redisMqPipe.ConnectInAsync<Event, EventResult>(rabbitMqPipe);
        await redisMqPipe.ConnectOutAsync(new EventHandlerWithResult());
    });
var serviceProvider = serviceCollection.BuildServiceProvider();
var mediator = await serviceProvider.GetRequiredService<IMediatorFactory>().CreateAsync();

// publish and send events
await mediator.PublishAsync(new Event());
await mediator.PublishAsync(new AnotherEvent());
var result = await mediator.SendAsync<Event, EventResult>(new Event());
```
