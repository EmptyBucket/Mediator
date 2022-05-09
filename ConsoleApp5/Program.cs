using FlexMediator;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

var serviceProvider = new ServiceCollection()
    .RegisterEasyNetQ("host=localhost")
    .AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost"))
    .AddMediator()
    .BuildServiceProvider();

// var mediator = serviceProvider.GetRequiredService<Mediator>();
// var pipeFactory = serviceProvider.GetRequiredService<IPipeFactory>();
//
// var rabbitMqPipe = pipeFactory.Create<RabbitMqPipe>();
// await rabbitMqPipe.ConnectOut<Event, EventResult>(mediator);
//
// var redisMqPipe = pipeFactory.Create<RedisMqPipe>();
// await redisMqPipe.ConnectOut<Event, EventResult>(rabbitMqPipe);
//
// var handlingPipe = pipeFactory.Create<HandlingPipe<Event, EventResult>>();
// await handlingPipe.ConnectOut<Event, EventResult>(redisMqPipe);
//
// await mediator.Publish(new Event("qwe"));
// var result = await mediator.Send<Event, EventResult>(new Event("qwe"));

const string redisChannel = "test";
var connectionMultiplexer = serviceProvider.GetRequiredService<IConnectionMultiplexer>();
var subscriber = connectionMultiplexer.GetSubscriber();

await subscriber.PublishAsync(redisChannel, new RedisValue("before"));
await Task.Delay(100);
var channelMessageQueue1 = await connectionMultiplexer.GetSubscriber().SubscribeAsync(redisChannel);
channelMessageQueue1.OnMessage(m =>
{
    Console.WriteLine("qwe");
});
await subscriber.PublishAsync(redisChannel, new RedisValue("sub 1"));
await Task.Delay(100);
var channelMessageQueue2 = await connectionMultiplexer.GetSubscriber().SubscribeAsync(redisChannel);
channelMessageQueue2.OnMessage(m =>
{
    Console.WriteLine("asd");
});
await subscriber.PublishAsync(redisChannel, new RedisValue("sub 2"));
await Task.Delay(100);
await channelMessageQueue1.UnsubscribeAsync();
await subscriber.PublishAsync(redisChannel, new RedisValue("unsub 1"));
await Task.Delay(100);
await channelMessageQueue2.UnsubscribeAsync();
await subscriber.PublishAsync(redisChannel, new RedisValue("unsub 2"));
await Task.Delay(100);


await Task.Delay(TimeSpan.FromHours(1));