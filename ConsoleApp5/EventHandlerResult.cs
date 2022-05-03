using FlexMediator;
using FlexMediator.Handlers;

namespace ConsoleApp5;

public class EventHandlerResult : IHandler<Event, string>
{
    public Task<string> HandleAsync(Event message, MessageOptions options, CancellationToken token)
    {
        return Task.FromResult(message.ToString());
    }
}