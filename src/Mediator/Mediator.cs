namespace Mediator;

internal class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PubSub.IConnectingPipe _publishPipe;
    private readonly RequestResponse.IConnectingPipe _sendPipe;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _publishPipe = new PubSub.ConnectingPipe();
        _sendPipe = new RequestResponse.ConnectingPipe();
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

    public Task<PubSub.PipeConnection> ConnectOutAsync<TMessage>(PubSub.IPipe pipe,
        string routingKey = "", string subscriptionId = "", CancellationToken token = default) =>
        _publishPipe.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionId, token);

    public Task<RequestResponse.PipeConnection> ConnectOutAsync<TMessage, TResult>(RequestResponse.IPipe pipe,
        string routingKey = "", CancellationToken token = default) =>
        _sendPipe.ConnectOutAsync<TMessage, TResult>(pipe, routingKey, token);

    public ValueTask DisposeAsync() => _publishPipe.DisposeAsync();
}