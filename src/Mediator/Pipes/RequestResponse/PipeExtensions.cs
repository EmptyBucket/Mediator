using Mediator.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Pipes.RequestResponse;

public static class PipeExtensions
{
    public static Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(this IReqPipeConnector pipeConnector,
        Func<IServiceProvider, IHandler<TMessage, TResult>> factory, string routingKey = "",
        CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync<TMessage, TResult>(new HandlingPipe<TMessage, TResult>(factory), routingKey, token);

    public static Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(this IReqPipeConnector pipeConnector,
        IHandler<TMessage, TResult> handler, string routingKey = "", CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync(_ => handler, routingKey, token: token);

    public static Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult, THandler>(this IReqPipeConnector pipeConnector,
        string routingKey = "", CancellationToken token = default)
        where THandler : IHandler<TMessage, TResult> =>
        pipeConnector.ConnectOutAsync(p => p.GetRequiredService<THandler>(), routingKey, token: token);

    public static Task<IAsyncDisposable> ConnectInAsync<TMessage, TResult>(this IReqPipe pipe, IReqPipeConnector pipeConnector,
        string routingKey = "", CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync<TMessage, TResult>(pipe, routingKey, token);
}