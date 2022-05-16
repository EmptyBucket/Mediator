using Mediator.Handlers;

namespace Mediator.Pipes.RequestResponse;

public interface IReqPipe
{
    Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> ctx, CancellationToken token = default);
}