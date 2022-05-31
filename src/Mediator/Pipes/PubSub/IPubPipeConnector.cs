namespace Mediator.Pipes;

public interface IPubPipeConnector : IAsyncDisposable
{
    IDisposable ConnectOut<TMessage>(IPubPipe pipe, string routingKey = "", string subscriptionId = "");

    Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "", string subscriptionId = "",
        CancellationToken token = default);
}