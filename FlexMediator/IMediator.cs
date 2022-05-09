using FlexMediator.Utils;

namespace FlexMediator;

public interface IMediator
{
    Task<TResult> SendAsync<TMessage, TResult>(TMessage message, Action<MessageOptions>? optionsBuilder = null,
        CancellationToken token = default);

    Task PublishAsync<TMessage>(TMessage message, Action<MessageOptions>? optionsBuilder = null,
        CancellationToken token = default);
}