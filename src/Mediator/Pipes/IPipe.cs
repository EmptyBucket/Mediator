using Mediator.Utils;

namespace Mediator.Pipes;

public interface IPipe
{
    Task PassAsync<TMessage>(TMessage message, MessageContext context,
        CancellationToken token = default);

    Task<TResult> PassAsync<TMessage, TResult>(TMessage message, MessageContext context,
        CancellationToken token = default);
}