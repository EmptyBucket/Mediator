namespace Mediator.Pipes.RequestResponse;

public interface ISendPipeConnector : IAsyncDisposable
{
    Task<SendPipeConnection> ConnectOutAsync<TMessage, TResult>(ISendPipe pipe, string routingKey = "",
        CancellationToken token = default);
}