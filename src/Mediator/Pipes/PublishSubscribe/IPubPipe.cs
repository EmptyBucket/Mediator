using Mediator.Handlers;

namespace Mediator.Pipes.PublishSubscribe;

public interface IPubPipe
{
    Task PassAsync<TMessage>(MessageContext<TMessage> ctx, CancellationToken token = default);
}