using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.PublishSubscribe;
using Mediator.Pipes.RequestResponse;

namespace Mediator;

internal class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConnectingPipe _pipe;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _pipe = new ConnectingPipe();
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

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default) =>
        _pipe.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionId, token);

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        _pipe.ConnectOutAsync<TMessage, TResult>(pipe, routingKey, token);

    public async ValueTask DisposeAsync() => await _pipe.DisposeAsync();
}