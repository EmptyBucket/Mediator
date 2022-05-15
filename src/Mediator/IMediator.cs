using Mediator.Handlers;
using Mediator.Pipes;

namespace Mediator;

public interface IMediator : IPipeConnector
{
    Task PublishAsync<TMessage>(TMessage message,
        Func<MessageContext<TMessage>, MessageContext<TMessage>>? contextBuilder = null,
        CancellationToken token = default);

    Task<TResult> SendAsync<TMessage, TResult>(TMessage message,
        Func<MessageContext<TMessage>, MessageContext<TMessage>>? contextBuilder = null,
        CancellationToken token = default);
}