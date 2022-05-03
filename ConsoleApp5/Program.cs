// See https://aka.ms/new-console-template for more information

using ConsoleApp5;
using ConsoleApp5.Models;
using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;

var bus = RabbitHutch.CreateBus("host=localhost");

var serviceCollection = new ServiceCollection();
var serviceProvider = serviceCollection.BuildServiceProvider();
var mediator = serviceProvider.GetRequiredService<IMediator>();
await mediator.Publish(new Event("qwe"));

namespace ConsoleApp5
{
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