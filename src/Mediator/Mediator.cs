using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.PubSub;
using Mediator.Pipes.RequestResponse;

namespace Mediator;

internal class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnectablePublishPipe _publishPipe;
    private readonly IConnectableSendPipe _sendPipe;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _publishPipe = new PublishPipe();
        _sendPipe = new SendPipe();
    }

    public async Task PublishAsync<TMessage>(TMessage message,
        Action<MessageContext>? contextBuilder = null, CancellationToken token = default)
    {
        var messageContext = new MessageContext(_serviceProvider);
        contextBuilder?.Invoke(messageContext);
        await _publishPipe.PassAsync(message, messageContext, token);
    }

    public async Task<TResult> SendAsync<TMessage, TResult>(TMessage message,
        Action<MessageContext>? contextBuilder = null, CancellationToken token = default)
    {
        var messageContext = new MessageContext(_serviceProvider);
        contextBuilder?.Invoke(messageContext);
        return await _sendPipe.PassAsync<TMessage, TResult>(message, messageContext, token);
    }

    public Task<PublishPipeConnection> ConnectOutAsync<TMessage>(IPublishPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default) =>
        _publishPipe.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionId, token);

    public Task<SendPipeConnection> ConnectOutAsync<TMessage, TResult>(ISendPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        _sendPipe.ConnectOutAsync<TMessage, TResult>(pipe, routingKey, token);

    public ValueTask DisposeAsync() => _publishPipe.DisposeAsync();
}