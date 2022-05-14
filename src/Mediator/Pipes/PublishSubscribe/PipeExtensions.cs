using Mediator.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Pipes.PublishSubscribe;

public static class PipeExtensions
{
    public static Task<IAsyncDisposable> ConnectOutAsync<TMessage>(this IPubPipeConnector pipeConnector,
        Func<IServiceProvider, IHandler<TMessage>> factory, string routingKey = "", string subscriptionId = "",
        CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync<TMessage>(new HandlingPipe<TMessage>(factory), routingKey, subscriptionId, token);

    public static Task<IAsyncDisposable> ConnectOutAsync<TMessage>(this IPubPipeConnector pipeConnector,
        IHandler<TMessage> handler, string routingKey = "", string subscriptionId = "",
        CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync(_ => handler, routingKey, subscriptionId, token: token);

    public static Task<IAsyncDisposable> ConnectOutAsync<TMessage, THandler>(this IPubPipeConnector pipeConnector,
        string routingKey = "", string subscriptionId = "", CancellationToken token = default)
        where THandler : IHandler<TMessage> =>
        pipeConnector.ConnectOutAsync(p => p.GetRequiredService<THandler>(), routingKey, subscriptionId, token: token);

    public static Task<IAsyncDisposable> ConnectInAsync<TMessage>(this IPubPipe pipe, IPubPipeConnector pipeConnector,
        string routingKey = "", string subscriptionId = "", CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionId, token);
}