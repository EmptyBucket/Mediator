using Mediator.Handlers;
using Mediator.Pipes.PublishSubscribe;
using Mediator.Pipes.RequestResponse;

namespace Mediator.Pipes;

public class ConnectingPipe : IConnectingPipe
{
    private readonly ConnectingPubPipe _connectingPubPipe;
    private readonly ConnectingReqPipe _connectingReqPipe;

    public ConnectingPipe(IServiceProvider serviceProvider)
    {
        _connectingPubPipe = new ConnectingPubPipe(serviceProvider);
        _connectingReqPipe = new ConnectingReqPipe(serviceProvider);
    }

    public Task PassAsync<TMessage>(MessageContext<TMessage> context, CancellationToken token = default) =>
        _connectingPubPipe.PassAsync(context, token);

    public Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> context,
        CancellationToken token = default) =>
        _connectingReqPipe.PassAsync<TMessage, TResult>(context, token);

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default) =>
        _connectingPubPipe.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionId, token);

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        _connectingReqPipe.ConnectOutAsync<TMessage, TResult>(pipe, routingKey, token);

    public async ValueTask DisposeAsync()
    {
        await _connectingPubPipe.DisposeAsync();
        await _connectingReqPipe.DisposeAsync();
    }
}