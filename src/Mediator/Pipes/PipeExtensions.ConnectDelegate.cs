using Mediator.Handlers;

namespace Mediator.Pipes;

public static partial class PipeExtensions
{
    public static IDisposable ConnectDelegate<TMessage>(this IPubPipeConnector pipeConnector,
        Func<MessageContext<TMessage>, CancellationToken, Task> func, string routingKey = "",
        string subscriptionId = "") =>
        pipeConnector.ConnectHandler(new LambdaHandler<TMessage>(func), routingKey, subscriptionId);

    public static IDisposable ConnectDelegate<TMessage, TResult>(this IReqPipeConnector pipeConnector,
        Func<MessageContext<TMessage>, CancellationToken, Task<TResult>> func, string routingKey = "") =>
        pipeConnector.ConnectHandler(new LambdaHandler<TMessage, TResult>(func), routingKey);
}