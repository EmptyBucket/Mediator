namespace FlexMediator.Pipes;

public static class PipeExtensions
{
    public static Task<PipeConnection> From<TMessage>(this IPipe pipe, IPipeConnector pipeConnector,
        string routingKey = "") =>
        pipeConnector.Into<TMessage>(pipe, routingKey);

    public static Task<PipeConnection> From<TMessage, TResult>(this IPipe pipe, IPipeConnector pipeConnector,
        string routingKey = "") =>
        pipeConnector.Into<TMessage, TResult>(pipe, routingKey);
}