// See https://aka.ms/new-console-template for more information

using ConsoleApp5;
using ConsoleApp5.Pipes;
using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var serviceCollection = new ServiceCollection();
var bus = RabbitHutch.CreateBus("host=localhost");
serviceCollection.AddMediator(
    t => t.AddTopology<Event, EventHandler>("rabbit"),
    t => t.AddTransport<RabbitMqPipe>("rabbit"));

var task = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(1_000);
    }
});


await task;

public record Event(string Name);

public class EventHandler : IHandler<Event>
{
    private readonly ILogger<EventHandler> _logger;

    public EventHandler(ILogger<EventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(Event message, MessageOptions options, CancellationToken token)
    {
        _logger.LogInformation("Handle event");
        return Task.CompletedTask;
    }
}