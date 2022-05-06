using FlexMediator.Utils;

namespace FlexMediator.Handlers;

public interface IHandler
{
    Task HandleAsync(object message, MessageOptions options, CancellationToken token);
}

public interface IHandler<in TMessage> : IHandler
{
    Task HandleAsync(TMessage message, MessageOptions options, CancellationToken token);

    async Task IHandler.HandleAsync(object message, MessageOptions options, CancellationToken token) =>
        await HandleAsync((TMessage)message, options, token);
}

public interface IHandler<in TMessage, TResult> : IHandler<TMessage>
{
    new Task<TResult> HandleAsync(TMessage message, MessageOptions options, CancellationToken token);

    Task IHandler<TMessage>.HandleAsync(TMessage message, MessageOptions options, CancellationToken token) =>
        HandleAsync(message, options, token);
}