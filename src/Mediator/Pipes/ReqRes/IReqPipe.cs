using Mediator.Handlers;

namespace Mediator.Pipes;

public interface IReqPipe
{
    Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> ctx, CancellationToken token = default);
}