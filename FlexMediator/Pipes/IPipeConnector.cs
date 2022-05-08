namespace FlexMediator.Pipes;

public interface IPipeConnector : IAsyncDisposable
{
    Task<PipeConnection> Into<TMessage>(IPipe pipe, string routingKey = "", CancellationToken token = default);

    Task<PipeConnection> Into<TMessage, TResult>(IPipe pipe, string routingKey = "", CancellationToken token = default);
}