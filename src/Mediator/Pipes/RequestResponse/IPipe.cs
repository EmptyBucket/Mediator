namespace Mediator.RequestResponse;

public interface IPipe
{
    Task<TResult> PassAsync<TMessage, TResult>(TMessage message, MessageContext context,
        CancellationToken token = default);
}