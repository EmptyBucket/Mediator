using FlexMediator.Pipes;
using FlexMediator.Utils;

namespace FlexMediator;

internal class Mediator : IMediator
{
    private readonly Pipe _pipe;

    public Mediator()
    {
        _pipe = new Pipe();
    }

    public async Task Publish<TMessage>(TMessage message, Action<MessageOptions>? optionsBuilder = null,
        CancellationToken token = default)
    {
        var messageOptions = new MessageOptions();
        optionsBuilder?.Invoke(messageOptions);
        await _pipe.Handle(message, messageOptions, token);
    }

    public async Task<TResult> Send<TMessage, TResult>(TMessage message, Action<MessageOptions>? optionsBuilder = null,
        CancellationToken token = default)
    {
        var messageOptions = new MessageOptions();
        optionsBuilder?.Invoke(messageOptions);
        return await _pipe.Handle<TMessage, TResult>(message, messageOptions, token);
    }

    public IPipeConnector PipeConnector => _pipe;
}