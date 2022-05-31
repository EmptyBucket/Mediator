namespace Mediator.Pipes;

public interface IReqPipeConnector : IAsyncDisposable
{
    IDisposable ConnectOut<TMessage, TResult>(IReqPipe pipe, string routingKey = "");

    Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default);
}