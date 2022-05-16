using Mediator.Handlers;
using Samples.Events;

namespace Samples.Handlers;

public class EventHandlerWithResult : IHandler<Event, EventResult>
{
    public Task<EventResult> HandleAsync(Event message, MessageContext<Event> ctx, CancellationToken token)
    {
        return Task.FromResult(new EventResult());
    }
}