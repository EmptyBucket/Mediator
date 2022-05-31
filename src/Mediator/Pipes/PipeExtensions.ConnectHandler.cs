using Mediator.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Pipes;

public static partial class PipeExtensions
{
    public static IDisposable ConnectHandler<TMessage>(this IPubPipeConnector pipeConnector,
        Func<IServiceProvider, IHandler<TMessage>> factory, string routingKey = "", string subscriptionId = "") =>
        pipeConnector.ConnectOut<TMessage>(new HandlingPipe<TMessage>(factory), routingKey, subscriptionId);

    public static IDisposable ConnectHandler<TMessage>(this IPubPipeConnector pipeConnector,
        IHandler<TMessage> handler, string routingKey = "", string subscriptionId = "") =>
        pipeConnector.ConnectHandler(_ => handler, routingKey, subscriptionId);

    public static IDisposable ConnectHandler<TMessage, THandler>(this IPubPipeConnector pipeConnector,
        string routingKey = "", string subscriptionId = "")
        where THandler : IHandler<TMessage> =>
        pipeConnector.ConnectHandler(p => p.GetRequiredService<THandler>(), routingKey, subscriptionId);

    public static IDisposable ConnectHandler<TMessage, TResult>(this IReqPipeConnector pipeConnector,
        Func<IServiceProvider, IHandler<TMessage, TResult>> factory, string routingKey = "") =>
        pipeConnector.ConnectOutAsync<TMessage, TResult>(new HandlingPipe<TMessage, TResult>(factory), routingKey);

    public static IDisposable ConnectHandler<TMessage, TResult>(this IReqPipeConnector pipeConnector,
        IHandler<TMessage, TResult> handler, string routingKey = "") =>
        pipeConnector.ConnectHandler(_ => handler, routingKey);

    public static IDisposable ConnectHandler<TMessage, TResult, THandler>(
        this IReqPipeConnector pipeConnector, string routingKey = "")
        where THandler : IHandler<TMessage, TResult> =>
        pipeConnector.ConnectHandler(p => p.GetRequiredService<THandler>(), routingKey);
}