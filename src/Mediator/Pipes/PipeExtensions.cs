namespace Mediator.Pipes;

public static class PipeExtensions
{
    public static Task<PipeConnection> ConnectInAsync<TMessage>(this IPipe pipe, IPipeConnector pipeConnector,
        string routingKey = "", string subscriptionId = "") =>
        pipeConnector.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionId);

    public static Task<PipeConnection> ConnectInAsync<TMessage, TResult>(this IPipe pipe, IPipeConnector pipeConnector,
        string routingKey = "") =>
        pipeConnector.ConnectOutAsync<TMessage, TResult>(pipe, routingKey);
}