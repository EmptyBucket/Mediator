using FlexMediator.Utils;

namespace FlexMediator.Pipes;

public interface IPipe
{
    Task PassAsync<TMessage>(TMessage message, MessageOptions options,
        CancellationToken token = default);

    Task<TResult> PassAsync<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token = default);
}