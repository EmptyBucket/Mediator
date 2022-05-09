using FlexMediator;
using FlexMediator.Utils;

namespace ConsoleApp5;

public record Event(string Name);

public record AnotherEvent(string Name);

public record EventResult(string Name);

public class EventHandler : IHandler<Event>
{
    public Task HandleAsync(Event message, MessageContext context, CancellationToken token)
    {
        return Task.FromResult(new EventResult(message.Name));
    }
}

public class EventHandlerWithResult : IHandler<Event, EventResult>
{
    public Task<EventResult> HandleAsync(Event message, MessageContext context, CancellationToken token)
    {
        return Task.FromResult(new EventResult(message.Name));
    }
}