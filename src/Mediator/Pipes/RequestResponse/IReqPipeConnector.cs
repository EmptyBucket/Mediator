namespace Mediator.Pipes.RequestResponse;

public interface IReqPipeConnector : IAsyncDisposable
{
    Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default);
}