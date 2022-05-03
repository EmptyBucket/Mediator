using FlexMediator.Pipes;

namespace FlexMediator;

internal class Mediator : IMediator
{
    private readonly IPipe _dispatchPipe;

    public Mediator(IPipe dispatchPipe, MediatorConfiguration mediatorConfiguration)
    {
        _dispatchPipe = dispatchPipe;
        Configuration = mediatorConfiguration;
    }

    public async Task Publish<TMessage>(TMessage message, Action<MessageOptions>? optionsBuilder = null,
        CancellationToken token = default)
    {
        var messageOptions = new MessageOptions();
        optionsBuilder?.Invoke(messageOptions);
        await _dispatchPipe.Handle(message, messageOptions, token);
    }

    public async Task<TResult> Send<TMessage, TResult>(TMessage message, Action<MessageOptions>? optionsBuilder = null,
        CancellationToken token = default)
    {
        var messageOptions = new MessageOptions();
        optionsBuilder?.Invoke(messageOptions);
        return await _dispatchPipe.Handle<TMessage, TResult>(message, messageOptions, token);
    }

    public MediatorConfiguration Configuration { get; }
}