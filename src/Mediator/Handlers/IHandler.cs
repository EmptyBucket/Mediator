namespace Mediator.Handlers;

public interface IHandler<TMessage>
{
    Task HandleAsync(TMessage message, MessageContext<TMessage> ctx, CancellationToken token);
}

public interface IHandler<TMessage, TResult> : IHandler<TMessage>
{
    new Task<TResult> HandleAsync(TMessage message, MessageContext<TMessage> ctx, CancellationToken token);

    Task IHandler<TMessage>.HandleAsync(TMessage message, MessageContext<TMessage> ctx, CancellationToken token) =>
        HandleAsync(message, ctx, token);
}