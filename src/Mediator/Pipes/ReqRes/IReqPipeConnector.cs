namespace Mediator.Pipes;

public interface IReqPipeConnector : IAsyncDisposable
{
    Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default);
}