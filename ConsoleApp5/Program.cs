// See https://aka.ms/new-console-template for more information

using ConsoleApp5;
using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;

var bus = RabbitHutch.CreateBus("host=localhost");
RabbitHutch.RegisterBus();

var serviceCollection = new ServiceCollection();
serviceCollection.AddMediator(m =>
{
    m.AddTopology<Event>(new EventHandler(), "rabbit");
    m.AddRabbitMqTransport(bus);
});
var serviceProvider = serviceCollection.BuildServiceProvider();
var mediator = serviceProvider.GetRequiredService<IMediator>();
await mediator.Publish(new Event("qwe"));

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
    public Task Handle(Event message, MessageOptions options, CancellationToken token)
    {
        Console.WriteLine(message.Name);
        return Task.CompletedTask;
    }
}