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
using Mediator.Pipes;
using Mediator.Pipes.RabbitMq;
using Mediator.Pipes.RedisMq;
using Microsoft.Extensions.DependencyInjection;
using Samples.Events;
using Samples.Handlers;
using StackExchange.Redis;
using EventHandler = Samples.Handlers.EventHandler;

var serviceProvider = new ServiceCollection()
    // register rabbitMq
    .RegisterEasyNetQ("host=localhost")
    // register redisMq
    .AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost"))
    .AddMediator(async (f, c) =>
    {
        // configure rabbitmq pipeline
        var rabbitMqPipe = f.Create<RabbitMqPipe>();

        await rabbitMqPipe.ConnectInAsync<Event>(c);
        await rabbitMqPipe.ConnectOutAsync(_ => new EventHandler(), subscriptionId: "1");
        await rabbitMqPipe.ConnectOutAsync(_ => new EventHandler(), subscriptionId: "2");

        await rabbitMqPipe.ConnectInAsync<Event, EventResult>(c);
        
        await rabbitMqPipe.ConnectInAsync<AnotherEvent>(c);

        // configure redisMq pipeline
        var redisMqPipe = f.Create<RedisMqPipe>();

        await redisMqPipe.ConnectInAsync<Event, EventResult>(rabbitMqPipe);
        await redisMqPipe.ConnectOutAsync(_ => new EventHandlerWithResult());

        await redisMqPipe.ConnectInAsync<AnotherEvent>(c);
        await redisMqPipe.ConnectOutAsync(_ => new AnotherEventHandler());
    })
    .BuildServiceProvider();
var mediator = await serviceProvider.GetRequiredService<IMediatorFactory>().CreateAsync();


await mediator.PublishAsync(new Event());
await mediator.PublishAsync(new AnotherEvent());
var result = await mediator.SendAsync<Event, EventResult>(new Event());

await Task.Delay(TimeSpan.FromHours(1));