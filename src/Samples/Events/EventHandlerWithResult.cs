using Mediator;
using Mediator.Utils;

namespace Samples.Events;

public class EventHandlerWithResult : IHandler<Event, EventResult>
{
    public Task<EventResult> HandleAsync(Event message, MessageContext context, CancellationToken token)
    {
        return Task.FromResult(new EventResult());
    }
}