namespace FlexMediator.Pipes;

public interface IPipeConnector
{
    Task<PipeConnection> Connect<TMessage>(IPipe pipe, string routingKey = "");

    Task<PipeConnection> Connect<TMessage, TResult>(IPipe pipe, string routingKey = "");
}