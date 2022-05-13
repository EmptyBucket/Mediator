using Mediator.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Pipes.PubSub;

public static class PipeExtensions
{
    public static Task<PublishPipeConnection> ConnectOutAsync<TMessage>(this IPublishPipeConnector pipeConnector,
        Func<IServiceProvider, IHandler<TMessage>> factory, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync<TMessage>(new HandlingPipe<TMessage>(factory), routingKey, subscriptionId, token);

    public static Task<PublishPipeConnection> ConnectOutAsync<TMessage>(this IPublishPipeConnector pipeConnector,
        IHandler<TMessage> handler, string routingKey = "", string subscriptionId = "",
        CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync(_ => handler, routingKey, subscriptionId, token: token);

    public static Task<PublishPipeConnection> ConnectOutAsync<TMessage, THandler>(
        this IPublishPipeConnector pipeConnector, string routingKey = "", string subscriptionId = "",
        CancellationToken token = default)
        where THandler : IHandler<TMessage> =>
        pipeConnector.ConnectOutAsync(p => p.GetRequiredService<THandler>(), routingKey, subscriptionId, token: token);

    public static Task<PublishPipeConnection> ConnectInAsync<TMessage>(this IPublishPipe pipe,
        IPublishPipeConnector pipeConnector, string routingKey = "", string subscriptionId = "",
        CancellationToken token = default) =>
        pipeConnector.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionId, token);
}