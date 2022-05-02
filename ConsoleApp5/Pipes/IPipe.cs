namespace ConsoleApp5.Pipes;

public interface IPipe
{
    Task Handle<TMessage>(TMessage message, MessageOptions options, CancellationToken token);
    
    Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options, CancellationToken token);
}