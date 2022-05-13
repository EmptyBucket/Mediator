using Mediator.Handlers;

namespace Mediator.Pipes.PubSub;

public interface IPublishPipe
{
    Task PassAsync<TMessage>(TMessage message, MessageContext context, CancellationToken token = default);
}