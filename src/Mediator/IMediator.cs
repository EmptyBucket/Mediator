using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.PubSub;
using Mediator.Pipes.RequestResponse;

namespace Mediator;

public interface IMediator : IPublishPipeConnector, ISendPipeConnector
{
    Task PublishAsync<TMessage>(TMessage message, Action<MessageContext>? contextBuilder = null,
        CancellationToken token = default);

    Task<TResult> SendAsync<TMessage, TResult>(TMessage message, Action<MessageContext>? contextBuilder = null,
        CancellationToken token = default);
}