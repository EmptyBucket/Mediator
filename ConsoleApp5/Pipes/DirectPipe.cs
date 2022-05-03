namespace ConsoleApp5.Pipes;

public class DirectPipe : IPipe
{
    private readonly IPipe _pipe;

    public DirectPipe(IPipe pipe)
    {
        _pipe = pipe;
    }

    public Task Handle<TMessage>(TMessage message, MessageOptions options, CancellationToken token)
    {
        return _pipe.Handle(message, options, token);
    }

    public Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options, CancellationToken token)
    {
        return _pipe.Handle<TMessage, TResult>(message, options, token);
    }
}