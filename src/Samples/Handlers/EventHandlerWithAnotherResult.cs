using Mediator.Handlers;
using Samples.Events;

namespace Samples.Handlers;

public class EventHandlerWithAnotherResult : IHandler<Event, EventAnotherResult>
{
    public Task<EventAnotherResult> HandleAsync(MessageContext<Event> ctx, CancellationToken token)
    {
        return Task.FromResult(new EventAnotherResult());
    }
}