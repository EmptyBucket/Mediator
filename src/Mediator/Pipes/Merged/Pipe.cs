using Mediator.Handlers;

namespace Mediator.Pipes;

public class Pipe : IConnectingPipe
{
    private readonly IConnectingPubPipe _connectingPubPipe;
    private readonly IConnectingReqPipe _connectingReqPipe;

    public Pipe(IServiceProvider serviceProvider)
        : this(new PubPipe(serviceProvider), new ReqPipe(serviceProvider))
    {
    }

    public Pipe(IConnectingPubPipe connectingPubPipe, IConnectingReqPipe connectingReqPipe)
    {
        _connectingPubPipe = connectingPubPipe;
        _connectingReqPipe = connectingReqPipe;
    }

    public Task PassAsync<TMessage>(MessageContext<TMessage> ctx, CancellationToken token = default) =>
        _connectingPubPipe.PassAsync(ctx, token);

    public Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> ctx,
        CancellationToken token = default) =>
        _connectingReqPipe.PassAsync<TMessage, TResult>(ctx, token);

    public IDisposable ConnectOut<TMessage>(IPubPipe pipe, string routingKey = "", string subscriptionId = "") =>
        _connectingPubPipe.ConnectOut<TMessage>(pipe, routingKey, subscriptionId);

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default) =>
        _connectingPubPipe.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionId, token);

    public IDisposable ConnectOut<TMessage, TResult>(IReqPipe pipe, string routingKey = "") =>
        _connectingReqPipe.ConnectOut<TMessage, TResult>(pipe, routingKey);

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        _connectingReqPipe.ConnectOutAsync<TMessage, TResult>(pipe, routingKey, token);

    public async ValueTask DisposeAsync()
    {
        await _connectingPubPipe.DisposeAsync();
        await _connectingReqPipe.DisposeAsync();
    }
}