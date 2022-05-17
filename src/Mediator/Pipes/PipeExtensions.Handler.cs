using Mediator.Handlers;
using Mediator.Pipes.PublishSubscribe;
using Mediator.Pipes.RequestResponse;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Pipes;

public static partial class PipeExtensions
{
    public static Task<IAsyncDisposable> ConnectHandlerAsync<TMessage>(this IPubPipeConnector pipeConnector,
        Func<IServiceProvider, IHandler<TMessage>> factory, string routingKey = "", string subscriptionId = "",
        CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync<TMessage>(new HandlingPipe<TMessage>(factory), routingKey, subscriptionId, token);

    public static Task<IAsyncDisposable> ConnectHandlerAsync<TMessage>(this IPubPipeConnector pipeConnector,
        IHandler<TMessage> handler, string routingKey = "", string subscriptionId = "",
        CancellationToken token = default) =>
        pipeConnector.ConnectHandlerAsync(_ => handler, routingKey, subscriptionId, token);

    public static Task<IAsyncDisposable> ConnectHandlerAsync<TMessage>(this IPubPipeConnector pipeConnector,
        Func<MessageContext<TMessage>, CancellationToken, Task> func, string routingKey = "", string subscriptionId = "", 
        CancellationToken token = default) =>
        pipeConnector.ConnectHandlerAsync(new LambdaHandler<TMessage>(func), routingKey, subscriptionId, token);

    public static Task<IAsyncDisposable> ConnectHandlerAsync<TMessage, THandler>(this IPubPipeConnector pipeConnector,
        string routingKey = "", string subscriptionId = "", CancellationToken token = default)
        where THandler : IHandler<TMessage> =>
        pipeConnector.ConnectHandlerAsync(p => p.GetRequiredService<THandler>(), routingKey, subscriptionId, token);

    public static Task<IAsyncDisposable> ConnectHandlerAsync<TMessage, TResult>(this IReqPipeConnector pipeConnector,
        Func<IServiceProvider, IHandler<TMessage, TResult>> factory, string routingKey = "",
        CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync<TMessage, TResult>(new HandlingPipe<TMessage, TResult>(factory), routingKey,
            token);

    public static Task<IAsyncDisposable> ConnectHandlerAsync<TMessage, TResult>(this IReqPipeConnector pipeConnector,
        IHandler<TMessage, TResult> handler, string routingKey = "", CancellationToken token = default) =>
        pipeConnector.ConnectHandlerAsync(_ => handler, routingKey, token);

    public static Task<IAsyncDisposable> ConnectHandlerAsync<TMessage, TResult>(this IReqPipeConnector pipeConnector,
        Func<MessageContext<TMessage>, CancellationToken, Task<TResult>> func, string routingKey = "",
        CancellationToken token = default) =>
        pipeConnector.ConnectHandlerAsync(new LambdaHandler<TMessage, TResult>(func), routingKey, token);

    public static Task<IAsyncDisposable> ConnectHandlerAsync<TMessage, TResult, THandler>(
        this IReqPipeConnector pipeConnector, string routingKey = "", CancellationToken token = default)
        where THandler : IHandler<TMessage, TResult> =>
        pipeConnector.ConnectHandlerAsync(p => p.GetRequiredService<THandler>(), routingKey, token);
}