using Mediator.Handlers;

namespace Mediator.Pipes.PublishSubscribe;

public interface IPubPipe
{
    Task PassAsync<TMessage>(TMessage message, MessageContext context, CancellationToken token = default);
}