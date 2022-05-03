using FlexMediator;
using FlexMediator.Handlers;

namespace ConsoleApp5;

public class EventHandler : IHandler<Event>
{
    public Task HandleAsync(Event message, MessageOptions options, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}