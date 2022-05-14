using Mediator.Handlers;
using Samples.Events;

namespace Samples.Handlers;

public class AnotherEventHandler : IHandler<AnotherEvent>
{
    public Task HandleAsync(AnotherEvent message, MessageContext context, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}