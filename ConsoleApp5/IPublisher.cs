namespace ConsoleApp5;

public interface IPublisher
{
    Task Publish<TMessage>(TMessage message, Action<MessageOptions>? optionsBuilder = null,
        CancellationToken token = default);
}