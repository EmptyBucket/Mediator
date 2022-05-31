namespace Mediator.Pipes;

public static partial class PipeExtensions
{
    public static IDisposable ConnectIn<TMessage>(this IPubPipe pipe, IPubPipeConnector pipeConnector,
        string routingKey = "", string subscriptionId = "") =>
        pipeConnector.ConnectOut<TMessage>(pipe, routingKey, subscriptionId);

    public static IDisposable ConnectIn<TMessage, TResult>(this IReqPipe pipe, IReqPipeConnector pipeConnector,
        string routingKey = "") =>
        pipeConnector.ConnectOut<TMessage, TResult>(pipe, routingKey);
}