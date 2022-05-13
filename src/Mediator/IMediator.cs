namespace Mediator;

public interface IMediator : PubSub.IPipeConnector, RequestResponse.IPipeConnector
{
    Task PublishAsync<TMessage>(TMessage message, Action<MessageContext>? contextBuilder = null,
        CancellationToken token = default);

    Task<TResult> SendAsync<TMessage, TResult>(TMessage message, Action<MessageContext>? contextBuilder = null,
        CancellationToken token = default);
}