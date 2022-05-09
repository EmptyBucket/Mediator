using FlexMediator.Pipes;
using FlexMediator.Utils;

namespace FlexMediator;

public class Mediator : IMediator, IPipeConnector
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Pipe _pipe;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _pipe = new Pipe();
    }

    public async Task PublishAsync<TMessage>(TMessage message,
        Action<MessageOptions>? optionsBuilder = null, CancellationToken token = default)
    {
        var messageOptions = new MessageOptions(_serviceProvider);
        optionsBuilder?.Invoke(messageOptions);
        await _pipe.PassAsync(message, messageOptions, token);
    }

    public async Task<TResult> SendAsync<TMessage, TResult>(TMessage message,
        Action<MessageOptions>? optionsBuilder = null, CancellationToken token = default)
    {
        var messageOptions = new MessageOptions(_serviceProvider);
        optionsBuilder?.Invoke(messageOptions);
        return await _pipe.PassAsync<TMessage, TResult>(message, messageOptions, token);
    }

    public Task<PipeConnection> ConnectInAsync<TMessage>(IPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        _pipe.ConnectInAsync<TMessage>(pipe, routingKey, token);

    public Task<PipeConnection> ConnectInAsync<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        _pipe.ConnectInAsync<TMessage, TResult>(pipe, routingKey, token);

    public ValueTask DisposeAsync() => _pipe.DisposeAsync();
}