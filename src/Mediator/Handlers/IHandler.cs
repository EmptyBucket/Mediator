namespace Mediator;

public interface IHandler<in TMessage>
{
    Task HandleAsync(TMessage message, MessageContext context, CancellationToken token);
}

public interface IHandler<in TMessage, TResult> : IHandler<TMessage>
{
    new Task<TResult> HandleAsync(TMessage message, MessageContext context, CancellationToken token);

    Task IHandler<TMessage>.HandleAsync(TMessage message, MessageContext context, CancellationToken token) =>
        HandleAsync(message, context, token);
}