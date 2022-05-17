namespace Mediator.Handlers;

public class LambdaHandler<TMessage> : IHandler<TMessage>
{
    private readonly Func<MessageContext<TMessage>, CancellationToken, Task> _func;

    public LambdaHandler(Func<MessageContext<TMessage>, CancellationToken, Task> func)
    {
        _func = func;
    }

    public Task HandleAsync(MessageContext<TMessage> ctx, CancellationToken token) => _func(ctx, token);
}

public class LambdaHandler<TMessage, TResult> : IHandler<TMessage, TResult>
{
    private readonly Func<MessageContext<TMessage>, CancellationToken, Task<TResult>> _func;

    public LambdaHandler(Func<MessageContext<TMessage>, CancellationToken, Task<TResult>> func)
    {
        _func = func;
    }

    public Task<TResult> HandleAsync(MessageContext<TMessage> ctx, CancellationToken token) => _func(ctx, token);
}