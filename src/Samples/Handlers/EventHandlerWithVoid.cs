using Mediator.Handlers;
using Samples.Events;
using Void = Mediator.Void;

namespace Samples.Handlers;

public class EventHandlerWithVoid : IHandler<Event, Void>
{
    public Task<Void> HandleAsync(MessageContext<Event> ctx, CancellationToken token)
    {
        return Task.FromResult(new Void());
    }
}