namespace ConsoleApp5;

public interface IHandler<in TMessage, TResult> : IHandler<TMessage>
{
    new Task<TResult> Handle(TMessage message, MessageOptions options, CancellationToken token);

    Task IHandler<TMessage>.Handle(TMessage message, MessageOptions options, CancellationToken token) =>
        Handle(message, options, token);
}

public interface IHandler<in TMessage> : IHandler
{
    Task Handle(TMessage message, MessageOptions options, CancellationToken token);

    async Task IHandler.Handle(object message, MessageOptions options, CancellationToken token) =>
        await Handle((TMessage)message, options, token);
}

public interface IHandler
{
    Task Handle(object message, MessageOptions options, CancellationToken token);
}