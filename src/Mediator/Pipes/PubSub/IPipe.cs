namespace Mediator.PubSub;

public interface IPipe
{
    Task PassAsync<TMessage>(TMessage message, MessageContext context, CancellationToken token = default);
}