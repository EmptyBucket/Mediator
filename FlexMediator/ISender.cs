using FlexMediator.Utils;

namespace FlexMediator;

public interface ISender
{
    Task<TResult> Send<TMessage, TResult>(TMessage message, Action<MessageOptions>? optionsBuilder = null,
        CancellationToken token = default);
}