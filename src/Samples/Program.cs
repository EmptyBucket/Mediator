// MIT License
// 
// Copyright (c) 2022 Alexey Politov
// https://github.com/EmptyBucket/Mediator
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Mediator;
using Mediator.Configurations;
using Mediator.Pipes;
using Mediator.RabbitMq.Pipes;
using Mediator.Redis.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Samples.Events;
using Samples.Handlers;
using StackExchange.Redis;
using Void = Mediator.Handlers.Void;

var serviceCollection = new ServiceCollection();

// Configure RabbitMq and Redis
{
    serviceCollection
        .RegisterEasyNetQ("host=localhost")
        .AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost"));
}

// Configure mediator
{
    // You can call AddMediator as many times as you like, adding to the configuration from different parts of your application
    serviceCollection.AddMediator(connectPipes: (_, _) => { });
}

// Configure pipe registrations
{
    // Pipe registrations are needed to simply get and store pipes and transfer them from one part of the application to another
    serviceCollection
        .AddSingleton<RabbitMqPipe>()
        .AddSingleton<RedisMqPipe>()
        .AddSingleton<RedisStreamPipe>()
        // They are also needed in order not to have an explicit dependency on infrastructure libs see #Pipe registration usage
        .AddKeyedSingleton<IMulticastPipe, RabbitMqPipe>("rabbit-mq")
        .AddKeyedSingleton<IPipe>("rabbit-mq", (p, k) => p.GetRequiredKeyedService<IMulticastPipe>(k))
        .AddKeyedSingleton<IMulticastPipe, RedisMqPipe>("redis-mq")
        .AddKeyedSingleton<IMulticastPubPipe, RedisStreamPipe>("rabbit-stream");
}

var serviceProvider0 = serviceCollection.BuildServiceProvider();

// Pipe registration usage
{
    // You can get pipe with explicit type
    serviceProvider0.GetRequiredService<RabbitMqPipe>();

    // You can get pipe with interface. The advantage of this method is that your client code does not depend on infrastructure libraries
    serviceProvider0.GetRequiredKeyedService<IPipe>("rabbit-mq");
    serviceProvider0.GetRequiredKeyedService<IMulticastPipe>("rabbit-mq");

    // Notice that the stream support only publish/subscribe model so it uses IMulticastPubPipe
    serviceProvider0.GetRequiredKeyedService<IMulticastPubPipe>("redis-stream");
}

await serviceProvider0.DisposeAsync();

// Configure pipe connections
{
    serviceCollection
        .AddKeyedSingleton<IMulticastPipe, RabbitMqPipe>("rabbit-mq")
        .AddKeyedSingleton<IMulticastPipe, RedisMqPipe>("redis-mq")
        .AddKeyedSingleton<IMulticastPubPipe, RedisStreamPipe>("redis-stream")
        .AddMediator((p, topology) =>
        {
            var (dispatchPipe, _) = topology;
            var rabbitMqPipe = p.GetRequiredKeyedService<IMulticastPipe>("rabbit-mq");
            var redisMqPipe = p.GetRequiredKeyedService<IMulticastPipe>("redis-mq");
            var redisStreamPipe = p.GetRequiredKeyedService<IMulticastPubPipe>("redis-stream");

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
}

var serviceProvider1 = serviceCollection.BuildServiceProvider();

// Dynamic configure pipe connections
{
    var rabbitMqPipe = serviceProvider1.GetRequiredKeyedService<IMulticastPipe>("rabbit-mq");

    // You can skip pipe connections configuration that was above in AddMediator and
    // configure IMediator on the fly
    var connection = await rabbitMqPipe.ConnectHandlerAsync(new FooEventHandler(), subscriptionId: "3");

    // You can disconnect any Pipe use Dispose[Async]
    await connection.DisposeAsync();
}

// Configure routing
{
    var mediator = serviceProvider1.GetRequiredService<IMediator>();
    var (dispatchPipe, receivePipe) = mediator.Topology;
    var rabbitMqPipe = serviceProvider1.GetRequiredKeyedService<IMulticastPipe>("rabbit-mq");

    // You can specify routingKey for routing in same type
    await dispatchPipe.ConnectOutAsync<FooEvent>(rabbitMqPipe, "foo-routing-key");
    // You can use receivePipe for a single point configuration receive topology
    await rabbitMqPipe.ConnectOutAsync<FooEvent>(receivePipe, "foo-routing-key", subscriptionId: "4");
    await receivePipe.ConnectHandlerAsync(new FooEventHandler(), "foo-routing-key");
}

// Publish and send events
{
    var mediator = serviceProvider1.GetRequiredService<IMediator>();

    // Mediator, MediatorTopology, Pipes, PipeConnections are thread safe
    await mediator.PublishAsync(new FooEvent());
    await mediator.PublishAsync(new FooEvent(), new Options { RoutingKey = "foo-routing-key" });
    var result = await mediator.SendAsync<FooEvent, FooResult>(new FooEvent());
    await mediator.SendAsync<FooEvent, Void>(new FooEvent());
}

await Task.Delay(TimeSpan.FromHours(1));