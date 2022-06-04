using Mediator.Handlers;

namespace Mediator;

public interface IMediator : IAsyncDisposable
{
    Task PublishAsync<TMessage>(TMessage message, Action<MessageContext<TMessage>>? ctxBuilder = null,
        CancellationToken token = default);

    Task<TResult> SendAsync<TMessage, TResult>(TMessage message, Action<MessageContext<TMessage>>? ctxBuilder = null,
        CancellationToken token = default);

    MediatorTopology Topology { get; }
}