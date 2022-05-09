namespace FlexMediator.Pipes;

public interface IPipeConnector : IAsyncDisposable
{
    Task<PipeConnection> ConnectInAsync<TMessage>(IPipe pipe, string routingKey = "",
        CancellationToken token = default);

    Task<PipeConnection> ConnectInAsync<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default);
}