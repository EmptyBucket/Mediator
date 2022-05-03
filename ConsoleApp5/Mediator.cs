using ConsoleApp5.Bindings;
using ConsoleApp5.Pipes;

namespace ConsoleApp5;

internal class Mediator : IMediator
{
    private readonly IPipe _dispatchPipe;
    private readonly IPipeBinder _dispatchPipeBinder;

    public Mediator()
    {
        var dispatchPipeBinder = new PipeBinder();
        _dispatchPipeBinder = dispatchPipeBinder;
        _dispatchPipe = new ForkPipe(dispatchPipeBinder);
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

    public MediatorConfiguration Configuration { get; } = new();
}