using FlexMediator;
using FlexMediator.Utils;

namespace ConsoleApp5;

public record Event;

public record EventResult;

public class EventHandler : IHandler<Event>
{
    public Task HandleAsync(Event message, MessageContext context, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}

public class EventHandlerWithResult : IHandler<Event, EventResult>
{
    public Task<EventResult> HandleAsync(Event message, MessageContext context, CancellationToken token)
    {
        return Task.FromResult(new EventResult());
    }
}

public record AnotherEvent(string Name);

public class AnotherEventHandler : IHandler<AnotherEvent>
{
    public Task HandleAsync(AnotherEvent message, MessageContext context, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}