using Mediator.Handlers;

namespace Mediator.Pipes.PublishSubscribe;

public interface IPubPipe
{
    Task PassAsync<TMessage>(MessageContext<TMessage> context, CancellationToken token = default);
}