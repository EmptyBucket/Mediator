namespace Mediator.Pipes;

public static class PipeExtensions
{
    public static Task<PipeConnection> ConnectInAsync<TMessage>(this IPipe pipe, IPipeConnector pipeConnector,
        string routingKey = "", string subscriptionId = "", CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionId, token);

    public static Task<PipeConnection> ConnectInAsync<TMessage, TResult>(this IPipe pipe, IPipeConnector pipeConnector,
        string routingKey = "", CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync<TMessage, TResult>(pipe, routingKey, token);
}