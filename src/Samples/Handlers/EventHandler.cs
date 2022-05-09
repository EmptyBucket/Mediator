using Mediator;
using Samples.Events;

namespace Samples.Handlers;

public class EventHandler : IHandler<Event>
{
    public Task HandleAsync(Event message, MessageContext context, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}