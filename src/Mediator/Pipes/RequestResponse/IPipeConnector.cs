namespace Mediator.RequestResponse;

public interface IPipeConnector : IAsyncDisposable
{
    Task<PipeConnection> ConnectOutAsync<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default);
}