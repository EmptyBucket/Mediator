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

﻿// See https://aka.ms/new-console-template for more information

using ConsoleApp5;
using FlexMediator;
using Microsoft.Extensions.DependencyInjection;
using EventHandler = ConsoleApp5.EventHandler;

var serviceCollection = new ServiceCollection();
serviceCollection.RegisterEasyNetQ("host=localhost")
    .AddMediator(async c =>
    {
        await c.Topologies["direct"].BindDispatch<Event>();
        await c.Topologies["direct"].BindReceive<Event, EventHandler>();

        await c.Topologies["direct"].BindDispatch<Event, string>();
        await c.Topologies["direct"].BindReceive(new EventHandlerResult());

        await c.Topologies["rabbitmq"].BindDispatch<RabbitMqEvent>();
        await c.Topologies["rabbitmq"].BindReceive<RabbitMqEvent, RabbitMqEventHandler>();

        await c.Topologies["rabbitmq"].BindDispatch<RabbitMqEvent, string>();
        await c.Topologies["rabbitmq"].BindReceive(new RabbitMqEventHandlerResult());
    })
    .AddScoped<EventHandler>()
    .AddScoped<RabbitMqEventHandler>();
var serviceProvider = serviceCollection.BuildServiceProvider();

var mediator = serviceProvider.GetRequiredService<IMediator>();
await mediator.Publish(new Event("qwe"));
var eventResult = await mediator.Send<Event, string>(new Event("qwe"));

await mediator.Publish(new RabbitMqEvent("qwe"));
var rabbitMqEventResult = await mediator.Send<RabbitMqEvent, string>(new RabbitMqEvent("qwe"));

await Task.Delay(10_000);