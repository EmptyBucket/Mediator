namespace FlexMediator.Pipes;

public interface IPipeConnector : IAsyncDisposable
{
    Task<PipeConnection> ConnectOutAsync<TMessage>(IPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default);

    Task<PipeConnection> ConnectOutAsync<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default);
}