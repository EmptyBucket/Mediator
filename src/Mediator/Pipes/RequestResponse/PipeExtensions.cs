using Mediator.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Pipes.RequestResponse;

public static class PipeExtensions
{
    public static Task<SendPipeConnection> ConnectOutAsync<TMessage, TResult>(this ISendPipeConnector pipeConnector,
        Func<IServiceProvider, IHandler<TMessage, TResult>> factory, string routingKey = "",
        CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync<TMessage, TResult>(new HandlingPipe<TMessage, TResult>(factory), routingKey, token);

    public static Task<SendPipeConnection> ConnectOutAsync<TMessage, TResult>(this ISendPipeConnector pipeConnector,
        IHandler<TMessage, TResult> handler, string routingKey = "",
        CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync(_ => handler, routingKey, token: token);

    public static Task<SendPipeConnection> ConnectOutAsync<TMessage, TResult, THandler>(
        this ISendPipeConnector pipeConnector, string routingKey = "",
        CancellationToken token = default)
        where THandler : IHandler<TMessage, TResult> =>
        pipeConnector.ConnectOutAsync(p => p.GetRequiredService<THandler>(), routingKey, token: token);

    public static Task<SendPipeConnection> ConnectInAsync<TMessage, TResult>(this ISendPipe pipe,
        ISendPipeConnector pipeConnector, string routingKey = "",
        CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync<TMessage, TResult>(pipe, routingKey, token);
}