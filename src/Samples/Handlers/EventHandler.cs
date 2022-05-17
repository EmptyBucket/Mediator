using Mediator.Handlers;
using Samples.Events;

namespace Samples.Handlers;

public class EventHandler : IHandler<Event>
{
    public Task HandleAsync(MessageContext<Event> ctx, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}