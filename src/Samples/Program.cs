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
using Mediator.Redis.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Samples.Events;
using Samples.Handlers;
using StackExchange.Redis;
using EventHandler = Samples.Handlers.EventHandler;

var serviceCollection = new ServiceCollection();
serviceCollection
    .RegisterEasyNetQ("host=localhost")
    .AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost"));
serviceCollection
    .AddMediator(b => b.BindRabbitMq().BindRedisMq(), async (p, c) =>
    {
        var pipeFactory = p.GetRequiredService<IPipeFactory>();

        // configure rabbitMq pipeline
        var rabbitMqPipe = pipeFactory.Create<IBranchingPipe>("rabbit");
        await rabbitMqPipe.ConnectInAsync<Event>(c);
        await rabbitMqPipe.ConnectInAsync<Event, EventResult>(c);
        await rabbitMqPipe.ConnectInAsync<AnotherEvent>(c);
        await rabbitMqPipe.ConnectOutAsync(_ => new EventHandler(), subscriptionId: "1");
        await rabbitMqPipe.ConnectOutAsync(_ => new EventHandler(), subscriptionId: "2");

        // configure redisMq pipeline
        var redisMqPipe = pipeFactory.Create<IBranchingPipe>("redis");
        await redisMqPipe.ConnectInAsync<Event, EventResult>(rabbitMqPipe);
        await redisMqPipe.ConnectInAsync<AnotherEvent>(c);
        await redisMqPipe.ConnectOutAsync(_ => new EventHandlerWithResult());
        await redisMqPipe.ConnectOutAsync(_ => new AnotherEventHandler());
    });
var serviceProvider = serviceCollection.BuildServiceProvider();
var mediator = await serviceProvider.GetRequiredService<IMediatorFactory>().CreateAsync();

await mediator.PublishAsync(new Event());
await mediator.PublishAsync(new AnotherEvent());
var result = await mediator.SendAsync<Event, EventResult>(new Event());

await Task.Delay(TimeSpan.FromHours(1));