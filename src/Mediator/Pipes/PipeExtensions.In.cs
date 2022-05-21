namespace Mediator.Pipes;

public static partial class PipeExtensions
{
    public static Task<IAsyncDisposable> ConnectInAsync<TMessage>(this IPubPipe pipe, IPubPipeConnector pipeConnector,
        string routingKey = "", string subscriptionId = "", CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionId, token);

    public static Task<IAsyncDisposable> ConnectInAsync<TMessage, TResult>(this IReqPipe pipe,
        IReqPipeConnector pipeConnector, string routingKey = "", CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync<TMessage, TResult>(pipe, routingKey, token);
}