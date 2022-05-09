using FlexMediator;
using FlexMediator.Utils;

namespace ConsoleApp5;

public record Event(string Name);

public record EventResult(string Name);

public class EventHandler : IHandler<Event, EventResult>
{
    public Task<EventResult> HandleAsync(Event message, MessageContext context, CancellationToken token)
    {
        return Task.FromResult(new EventResult(message.Name));
    }
}