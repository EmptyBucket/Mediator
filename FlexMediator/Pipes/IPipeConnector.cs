namespace FlexMediator.Pipes;

public interface IPipeConnector
{
    Task<PipeConnection> Out<TMessage>(IPipe pipe, string routingKey = "");

    Task<PipeConnection> Out<TMessage, TResult>(IPipe pipe, string routingKey = "");
}