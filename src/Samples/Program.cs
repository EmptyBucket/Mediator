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
using Mediator.RabbitMq.Configurations;
using Mediator.RabbitMq.Pipes;
using Mediator.Redis.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Samples.Events;
using Samples.Handlers;
using StackExchange.Redis;
using Void = Mediator.Handlers.Void;

var serviceCollection = new ServiceCollection();

{
    // Configure RabbitMq and Redis
    serviceCollection
        .RegisterEasyNetQ("host=localhost")
        .AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost"));
}

{
    // Configure mediator
    // You can call AddMediator as many times as you like,
    // adding to the configuration from different parts of your application
    serviceCollection
        .AddMediator(bindPipes: _ => { }, connectPipes: (_, _) => { });
}

{
    // Configure pipe bindings
    serviceCollection
        .AddMediator(bind =>
        {
            // Pipe bindings are needed in order not to have an explicit dependency on
            // infrastructure libs
            // Bindings register a type to itself and all its IPubPipe and IReqPipe interfaces,
            // e.g. call BindRabbitMq() will bind:
            // {IPipe, IPubPipe, IReqPipe, IMulticastPipe, IMulticastPubPipe, IMulticastReqPipe} + nameof(RabbitMqPipe) => RabbitMqPipe
            // RabbitMqPipe => RabbitMqPipe
            bind.BindRabbitMq().BindRedis();
        });
}

var serviceProvider0 = serviceCollection.BuildServiceProvider();

{
    // Bindings usage
    var mediator = serviceProvider0.GetRequiredService<IMediator>();
    var (_, _, pipeFactory) = mediator.Topology;

    // You can create pipe with explicit type
    IPipe rabbitMqPipe = pipeFactory.Create<RabbitMqPipe>();

    // You can create pipe with interface, but then you must specify name
    rabbitMqPipe = pipeFactory.Create<IPipe>("RabbitMqPipe");
    rabbitMqPipe = pipeFactory.Create<IMulticastPipe>("RabbitMqPipe");

    // Pipe and HandlingPipe are also bound by default
    var pipe = pipeFactory.Create<MulticastPipe>();

    // Notice that the stream support only publish/subscribe model so it uses IMulticastPubPipe
    var redisStreamPipe = pipeFactory.Create<IMulticastPubPipe>("RedisStreamPipe");
}

await serviceProvider0.DisposeAsync();

{
    // Configure pipe connections
    serviceCollection
        .AddMediator((provider, topology) =>
        {
            var (dispatchPipe, _, pipeFactory) = topology;
            var rabbitMqPipe = pipeFactory.Create<IMulticastPipe>("RabbitMqPipe");
            var redisMqPipe = pipeFactory.Create<IMulticastPipe>("RedisMqPipe");
            var redisStreamPipe = pipeFactory.Create<IMulticastPubPipe>("RedisStreamPipe");

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

{
    // Dynamic configure pipe connections
    var mediator = serviceProvider1.GetRequiredService<IMediator>();
    var (_, _, pipeFactory) = mediator.Topology;
    var rabbitMqPipe = pipeFactory.Create<IMulticastPipe>("RabbitMqPipe");

    // You can skip pipe connections configuration that was above in AddMediator and
    // configure IMediator on the fly
    var connection = await rabbitMqPipe.ConnectHandlerAsync(new FooEventHandler(), subscriptionId: "3");

    // You can disconnect any Pipe use Dispose[Async]
    await connection.DisposeAsync();
}

{
    // Configure routing
    var mediator = serviceProvider1.GetRequiredService<IMediator>();
    var (dispatchPipe, receivePipe, pipeFactory) = mediator.Topology;
    var rabbitMqPipe = pipeFactory.Create<IMulticastPipe>("RabbitMqPipe");

    // You can specify routingKey for routing in same type
    await dispatchPipe.ConnectOutAsync<FooEvent>(rabbitMqPipe, "foo-routing-key");
    // You can use receivePipe for a single point configuration receive topology
    await receivePipe.ConnectInAsync<FooEvent>(rabbitMqPipe, "foo-routing-key", subscriptionId: "4");
    await receivePipe.ConnectHandlerAsync(new FooEventHandler(), "foo-routing-key");
}

{
    // Publish and send events
    var mediator = serviceProvider1.GetRequiredService<IMediator>();

    // Mediator, MediatorTopology, Pipes, PipeConnections are thread safe
    await mediator.PublishAsync(new FooEvent());
    await mediator.PublishAsync(new FooEvent(), new Options { RoutingKey = "foo-routing-key" });
    var result = await mediator.SendAsync<FooEvent, FooResult>(new FooEvent());
    await mediator.SendAsync<FooEvent, Void>(new FooEvent());
}

await Task.Delay(TimeSpan.FromHours(1));