using FlexMediator;
using FlexMediator.Handlers;

namespace ConsoleApp5;

public class RabbitMqEventHandler : IHandler<RabbitMqEvent>
{
    public Task HandleAsync(RabbitMqEvent message, MessageOptions options, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}