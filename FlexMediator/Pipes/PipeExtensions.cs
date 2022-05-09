namespace FlexMediator.Pipes;

public static class PipeExtensions
{
    public static Task<PipeConnection> ConnectOutAsync<TMessage>(this IPipe pipe, IPipeConnector pipeConnector,
        string routingKey = "") =>
        pipeConnector.ConnectInAsync<TMessage>(pipe, routingKey);

    public static Task<PipeConnection> ConnectOutAsync<TMessage, TResult>(this IPipe pipe, IPipeConnector pipeConnector,
        string routingKey = "") =>
        pipeConnector.ConnectInAsync<TMessage, TResult>(pipe, routingKey);
}