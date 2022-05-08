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

using ConsoleApp5;
using FlexMediator;
using FlexMediator.Pipes;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using EventHandler = ConsoleApp5.EventHandler;

var serviceProvider = new ServiceCollection()
    .RegisterEasyNetQ("host=localhost")
    .AddSingleton<ISubscriber>(_ => ConnectionMultiplexer.Connect("localhost").GetSubscriber())
    .AddMediator()
    .BuildServiceProvider();

var mediator = serviceProvider.GetRequiredService<IMediator>();
var pipeFactory = serviceProvider.GetRequiredService<IPipeFactory>();

var rabbitMqPipe = pipeFactory.Create<RabbitMqPipe>();
await rabbitMqPipe.In<Event, EventResult>(mediator.PipeConnector);

var redisMqPipe = pipeFactory.Create<RedisMqPipe>();
await redisMqPipe.In<Event, EventResult>(rabbitMqPipe);

//todo попробовать сделать так, чтобы при падении одного сообщения не запускались все хендлеры на ретрае
var handlingPipe = pipeFactory.Create<HandlingPipe>();
await handlingPipe.In<Event, EventResult>(redisMqPipe);
handlingPipe.BindHandler<Event, EventResult>(_ => new EventHandler());

await mediator.Publish(new Event("qwe"));
var result = await mediator.Send<Event, EventResult>(new Event("qwe"));

await Task.Delay(10_000);