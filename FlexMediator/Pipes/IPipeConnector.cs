namespace FlexMediator.Pipes;

public interface IPipeConnector
{
    Task<PipeConnection> Out<TMessage>(IPipe pipe, string routingKey = "", CancellationToken token = default);

    Task<PipeConnection> Out<TMessage, TResult>(IPipe pipe, string routingKey = "", CancellationToken token = default);
}