using FlexMediator.Utils;

namespace FlexMediator;

public interface IPublisher
{
    Task Publish<TMessage>(TMessage message, Action<MessageOptions>? optionsBuilder = null,
        CancellationToken token = default);
}