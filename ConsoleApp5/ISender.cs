namespace ConsoleApp5;

public interface ISender
{
    Task<TResult> Send<TMessage, TResult>(TMessage message, Action<MessageOptions>? optionsBuilder = null,
        CancellationToken token = default);
}