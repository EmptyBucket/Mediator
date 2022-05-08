using FlexMediator.Pipes;
using FlexMediator.Utils;

namespace FlexMediator;

public class Mediator : IMediator, IPipeConnector
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

    public Task<PipeConnection> Into<TMessage>(IPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        _pipe.Into<TMessage>(pipe, routingKey, token);

    public Task<PipeConnection> Into<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        _pipe.Into<TMessage, TResult>(pipe, routingKey, token);

    public ValueTask DisposeAsync() => _pipe.DisposeAsync();
}