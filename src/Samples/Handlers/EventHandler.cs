using Mediator.Handlers;
using Samples.Events;

namespace Samples.Handlers;

public class EventHandler : IHandler<Event>
{
    public Task HandleAsync(Event message, MessageContext<Event> context, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}