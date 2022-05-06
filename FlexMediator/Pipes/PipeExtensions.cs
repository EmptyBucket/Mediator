namespace FlexMediator.Pipes;

public static class PipeExtensions
{
    public static Task<PipeConnection> In<TMessage>(this IPipe pipe, IPipeConnector pipeConnector,
        string routingKey = "") =>
        pipeConnector.Out<TMessage>(pipe, routingKey);

    public static Task<PipeConnection> In<TMessage, TResult>(this IPipe pipe, IPipeConnector pipeConnector,
        string routingKey = "") =>
        pipeConnector.Out<TMessage, TResult>(pipe, routingKey);
}