using FlexMediator.Pipes;
using FlexMediator.Utils;

namespace FlexMediator;

public interface IMediator : IPipeConnector
{
    Task<TResult> SendAsync<TMessage, TResult>(TMessage message, Action<MessageContext>? contextBuilder = null,
        CancellationToken token = default);

    Task PublishAsync<TMessage>(TMessage message, Action<MessageContext>? contextBuilder = null,
        CancellationToken token = default);
}