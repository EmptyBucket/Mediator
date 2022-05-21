namespace Mediator.Pipes;

public interface IPubPipeConnector : IAsyncDisposable
{
    Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "", string subscriptionId = "",
        CancellationToken token = default);
}