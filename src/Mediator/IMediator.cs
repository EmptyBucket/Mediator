namespace Mediator;

public interface IMediator : IAsyncDisposable
{
    Task PublishAsync<TMessage>(TMessage message, Options? options = null,
        CancellationToken token = default);

    Task<TResult> SendAsync<TMessage, TResult>(TMessage message, Options? options = null,
        CancellationToken token = default);

    MediatorTopology Topology { get; }
}