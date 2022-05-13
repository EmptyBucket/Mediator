namespace Mediator.PubSub;

public interface IPipeConnector : IAsyncDisposable
{
    Task<PipeConnection> ConnectOutAsync<TMessage>(IPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default);
}