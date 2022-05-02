// See https://aka.ms/new-console-template for more information

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