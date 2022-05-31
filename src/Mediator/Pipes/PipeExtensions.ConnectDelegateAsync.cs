using Mediator.Handlers;

namespace Mediator.Pipes;

public static partial class PipeExtensions
{
    public static Task<IAsyncDisposable> ConnectDelegateAsync<TMessage>(this IPubPipeConnector pipeConnector,
        Func<MessageContext<TMessage>, CancellationToken, Task> func, string routingKey = "",
        string subscriptionId = "",
        CancellationToken token = default) =>
        pipeConnector.ConnectHandlerAsync(new LambdaHandler<TMessage>(func), routingKey, subscriptionId, token);
    
    public static Task<IAsyncDisposable> ConnectDelegateAsync<TMessage, TResult>(this IReqPipeConnector pipeConnector,
        Func<MessageContext<TMessage>, CancellationToken, Task<TResult>> func, string routingKey = "",
        CancellationToken token = default) =>
        pipeConnector.ConnectHandlerAsync(new LambdaHandler<TMessage, TResult>(func), routingKey, token);
}