using Mediator.Handlers;
using Mediator.Pipes;

namespace Mediator;

internal class Mediator : IMediator
{
    private readonly ConnectingPipe _dispatchPipe;
    private readonly ConnectingPipe _receivePipe;

    public Mediator(ConnectingPipe dispatchPipe, ConnectingPipe receivePipe)
    {
        _dispatchPipe = dispatchPipe;
        _receivePipe = receivePipe;
    }

    public async Task PublishAsync<TMessage>(TMessage message,
        Func<MessageContext<TMessage>, MessageContext<TMessage>>? ctxBuilder = null, CancellationToken token = default)
    {
        var route = Route.For<TMessage>();
        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var ctx = new MessageContext<TMessage>(route, messageId, correlationId, DateTimeOffset.Now, message);
        ctxBuilder?.Invoke(ctx);
        await _dispatchPipe.PassAsync(ctx, token);
    }

    public async Task<TResult> SendAsync<TMessage, TResult>(TMessage message,
        Func<MessageContext<TMessage>, MessageContext<TMessage>>? ctxBuilder = null, CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>();
        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var ctx = new MessageContext<TMessage>(route, messageId, correlationId, DateTimeOffset.Now, message);
        ctxBuilder?.Invoke(ctx);
        return await _dispatchPipe.PassAsync<TMessage, TResult>(ctx, token);
    }

    public (IConnectingPipe Dispatch, IConnectingPipe Receive) Topology => (_dispatchPipe, _receivePipe);

    public async ValueTask DisposeAsync()
    {
        await _dispatchPipe.DisposeAsync();
        await _receivePipe.DisposeAsync();
    }
}