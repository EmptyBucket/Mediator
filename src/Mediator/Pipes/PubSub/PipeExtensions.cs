using Microsoft.Extensions.DependencyInjection;

namespace Mediator.PubSub;

public static class PipeExtensions
{
    public static Task<PipeConnection> ConnectOutAsync<TMessage>(this IPipeConnector pipeConnector,
        Func<IServiceProvider, IHandler<TMessage>> factory, string routingKey = "", string subscriptionId = "",
        CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync<TMessage>(new HandlingPipe<TMessage>(factory), routingKey, subscriptionId, token);

    public static Task<PipeConnection> ConnectOutAsync<TMessage>(this IPipeConnector pipeConnector,
        IHandler<TMessage> handler, string routingKey = "", string subscriptionId = "",
        CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync(_ => handler, routingKey, subscriptionId, token: token);

    public static Task<PipeConnection> ConnectOutAsync<TMessage, THandler>(this IPipeConnector pipeConnector,
        string routingKey = "", string subscriptionId = "", CancellationToken token = default)
        where THandler : IHandler<TMessage> =>
        pipeConnector.ConnectOutAsync(p => p.GetRequiredService<THandler>(), routingKey, subscriptionId, token: token);

    public static Task<PipeConnection> ConnectInAsync<TMessage>(this IPipe pipe, IPipeConnector pipeConnector,
        string routingKey = "", string subscriptionId = "", CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionId, token);
}