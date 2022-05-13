using Microsoft.Extensions.DependencyInjection;

namespace Mediator.RequestResponse;

public static class PipeExtensions
{
    public static Task<PipeConnection> ConnectOutAsync<TMessage, TResult>(this IPipeConnector pipeConnector,
        Func<IServiceProvider, IHandler<TMessage, TResult>> factory, string routingKey = "",
        CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync<TMessage, TResult>(new HandlingPipe<TMessage, TResult>(factory), routingKey,
            token);

    public static Task<PipeConnection> ConnectOutAsync<TMessage, TResult>(this IPipeConnector pipeConnector,
        IHandler<TMessage, TResult> handler, string routingKey = "", CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync(_ => handler, routingKey, token: token);

    public static Task<PipeConnection> ConnectOutAsync<TMessage, TResult, THandler>(this IPipeConnector pipeConnector,
        string routingKey = "", CancellationToken token = default)
        where THandler : IHandler<TMessage, TResult> =>
        pipeConnector.ConnectOutAsync(p => p.GetRequiredService<THandler>(), routingKey, token: token);

    public static Task<PipeConnection> ConnectInAsync<TMessage, TResult>(this IPipe pipe, IPipeConnector pipeConnector,
        string routingKey = "", CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync<TMessage, TResult>(pipe, routingKey, token);
}