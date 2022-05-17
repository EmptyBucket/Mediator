using Mediator.Handlers;
using Mediator.Pipes;

namespace Mediator;

public interface IMediator : IAsyncDisposable
{
    Task PublishAsync<TMessage>(TMessage message,
        Func<MessageContext<TMessage>, MessageContext<TMessage>>? ctxBuilder = null, CancellationToken token = default);

    Task<TResult> SendAsync<TMessage, TResult>(TMessage message,
        Func<MessageContext<TMessage>, MessageContext<TMessage>>? ctxBuilder = null, CancellationToken token = default);

    (IConnectingPipe Dispatch, IConnectingPipe Receive) Topology { get; }
}