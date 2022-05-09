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