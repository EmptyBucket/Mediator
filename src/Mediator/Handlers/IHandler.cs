namespace Mediator.Handlers;

public interface IHandler<TMessage>
{
    Task HandleAsync(MessageContext<TMessage> ctx, CancellationToken token);
}

public interface IHandler<TMessage, TResult> : IHandler<TMessage>
{
    new Task<TResult> HandleAsync(MessageContext<TMessage> ctx, CancellationToken token);

    Task IHandler<TMessage>.HandleAsync(MessageContext<TMessage> ctx, CancellationToken token) =>
        HandleAsync(ctx, token);
}