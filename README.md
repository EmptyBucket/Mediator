# Mediator
Transparent mediator, allows messages to be routed according to the configured topology. Can integrate with ```rabbitmq``` or ```redismq```. Supports ```pub/sub```, ```request/response``` models and dynamic configuration
### Usage
An example of how a pipeline can be configured:
* mediator =[Event]> rabbitmq =[Event]> EventHandler#1
* mediator =[Event]> rabbitmq =[Event]> EventHandler#2
* mediator =[Event]> rabbitmq =[Event]> redismq =[Event]> EventHandler#result =[EventResult]> result
* mediator =[AnotherEvent]> redismq =[AnotherEvent]> AnotherEventHandler
```csharp
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
        await rabbitMqPipe.ConnectOutAsync(new EventHandler(), subscriptionId: "1");
        await rabbitMqPipe.ConnectOutAsync(new EventHandler(), subscriptionId: "2");

        // configure redisMq pipeline
        var redisMqPipe = pipeFactory.Create<IBranchingPipe>("redis");
        // it is possible to direct one pipe to another
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
```
