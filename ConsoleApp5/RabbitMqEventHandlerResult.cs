using FlexMediator;
using FlexMediator.Handlers;

namespace ConsoleApp5;

public class RabbitMqEventHandlerResult : IHandler<RabbitMqEvent, string>
{
    public Task<string> HandleAsync(RabbitMqEvent message, MessageOptions options, CancellationToken token)
    {
        return Task.FromResult(message.ToString());
    }
}