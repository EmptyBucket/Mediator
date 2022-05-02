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
using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;
using EventHandler = ConsoleApp5.EventHandler;

var bus = RabbitHutch.CreateBus("host=localhost");
RabbitHutch.RegisterBus();

var serviceCollection = new ServiceCollection();
serviceCollection.AddMediator(m =>
{
    m.AddTopology<Event>(new EventHandler(), "rabbit");
});
var serviceProvider = serviceCollection.BuildServiceProvider();
var mediator = serviceProvider.GetRequiredService<IMediator>();
await mediator.Publish(new Event("qwe"));

namespace ConsoleApp5
{
    // var task = Task.Run(async () =>
// {
//     while (true)
//     {
//         await Task.Delay(1_000);
//     }
// });
// await task;

    public record Event(string Name);

    public class EventHandler : IHandler<Event>
    {
        public Task HandleAsync(Event message, MessageOptions options, CancellationToken token)
        {
            Console.WriteLine(message.Name);
            return Task.CompletedTask;
        }
    }
}