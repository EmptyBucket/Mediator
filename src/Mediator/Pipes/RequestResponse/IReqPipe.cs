using Mediator.Handlers;

namespace Mediator.Pipes.RequestResponse;

public interface IReqPipe
{
    Task<TResult> PassAsync<TMessage, TResult>(TMessage message, MessageContext context,
        CancellationToken token = default);
}