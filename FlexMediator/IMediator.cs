using FlexMediator.Utils;

namespace FlexMediator;

public interface IMediator
{
    Task<TResult> SendAsync<TMessage, TResult>(TMessage message, Action<MessageContext>? optionsBuilder = null,
        CancellationToken token = default);

    Task PublishAsync<TMessage>(TMessage message, Action<MessageContext>? optionsBuilder = null,
        CancellationToken token = default);
}