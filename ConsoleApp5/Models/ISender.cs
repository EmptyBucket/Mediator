namespace ConsoleApp5.Models;

public interface ISender
{
    Task<TResult> Send<TMessage, TResult>(TMessage message, Action<MessageOptions>? optionsBuilder = null,
        CancellationToken token = default);
}