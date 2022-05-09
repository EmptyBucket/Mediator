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
        Action<MessageContext>? contextBuilder = null, CancellationToken token = default)
    {
        var messageContext = new MessageContext(_serviceProvider);
        contextBuilder?.Invoke(messageContext);
        await _pipe.PassAsync(message, messageContext, token);
    }

    public async Task<TResult> SendAsync<TMessage, TResult>(TMessage message,
        Action<MessageContext>? contextBuilder = null, CancellationToken token = default)
    {
        var messageContext = new MessageContext(_serviceProvider);
        contextBuilder?.Invoke(messageContext);
        return await _pipe.PassAsync<TMessage, TResult>(message, messageContext, token);
    }

    public Task<PipeConnection> ConnectOutAsync<TMessage>(IPipe pipe, string routingKey = "",
        string subscriptionName = "", CancellationToken token = default) =>
        _pipe.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionName, token);

    public Task<PipeConnection> ConnectOutAsync<TMessage, TResult>(IPipe pipe, string routingKey = "",
        string subscriptionName = "", CancellationToken token = default) =>
        _pipe.ConnectOutAsync<TMessage, TResult>(pipe, routingKey, subscriptionName, token);

    public ValueTask DisposeAsync() => _pipe.DisposeAsync();
}