using Mediator.Handlers;
using Mediator.Pipes;

namespace Mediator;

internal class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnectablePipe _connectablePipe;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _connectablePipe = new ConnectablePipe();
    }

    public async Task PublishAsync<TMessage>(TMessage message,
        Action<MessageContext>? contextBuilder = null, CancellationToken token = default)
    {
        var messageContext = new MessageContext(_serviceProvider);
        contextBuilder?.Invoke(messageContext);
        await _connectablePipe.PassAsync(message, messageContext, token);
    }

    public async Task<TResult> SendAsync<TMessage, TResult>(TMessage message,
        Action<MessageContext>? contextBuilder = null, CancellationToken token = default)
    {
        var messageContext = new MessageContext(_serviceProvider);
        contextBuilder?.Invoke(messageContext);
        return await _connectablePipe.PassAsync<TMessage, TResult>(message, messageContext, token);
    }

    public Task<PipeConnection> ConnectOutAsync<TMessage>(IPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default) =>
        _connectablePipe.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionId, token);

    public Task<PipeConnection> ConnectOutAsync<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        _connectablePipe.ConnectOutAsync<TMessage, TResult>(pipe, routingKey, token);

    public ValueTask DisposeAsync() => _connectablePipe.DisposeAsync();
}