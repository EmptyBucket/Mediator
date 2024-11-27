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
    // You can call AddMediator as many times as you like,
    // adding to the configuration from different parts of your application
    serviceCollection
        .AddMediator(bindPipes: _ => { }, connectPipes: (_, _) => { });
}

// Configure pipe bindings
{
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
}

var serviceProvider0 = serviceCollection.BuildServiceProvider();

// Bindings usage
{
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
}

await serviceProvider0.DisposeAsync();

// Configure pipe connections
{
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
}

var serviceProvider1 = serviceCollection.BuildServiceProvider();

// Dynamic configure pipe connections
{
    var mediator = serviceProvider1.GetRequiredService<IMediator>();
    var (_, _, pipeProvider) = mediator.Topology;
    var rabbitMqPipe = pipeProvider.Get<IMulticastPipe>("RabbitMqPipe");

    // You can skip pipe connections configuration that was above in AddMediator and
    // configure IMediator on the fly
    var connection = await rabbitMqPipe.ConnectHandlerAsync(new FooEventHandler(), subscriptionId: "3");

    // You can disconnect any Pipe use Dispose[Async]
    await connection.DisposeAsync();
}

// Configure routing
{
    var mediator = serviceProvider1.GetRequiredService<IMediator>();
    var (dispatchPipe, receivePipe, pipeProvider) = mediator.Topology;
    var rabbitMqPipe = pipeProvider.Get<IMulticastPipe>("RabbitMqPipe");

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