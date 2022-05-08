using FlexMediator.Handlers;
using FlexMediator.Utils;

namespace ConsoleApp5;

public record Event(string Name);

public class EventHandler : IHandler<Event, string>
{
    public Task<string> HandleAsync(Event message, MessageOptions options, CancellationToken token)
    {
        return Task.FromResult(message.Name);
    }
}