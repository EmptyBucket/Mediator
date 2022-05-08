using FlexMediator.Utils;

namespace FlexMediator.Pipes;

public interface IPipe
{
    Task Handle<TMessage>(TMessage message, MessageOptions options,
        CancellationToken token = default);

    Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token = default);
}