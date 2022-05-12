using Mediator.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Pipes;

public static class PipeConnectorExtensions
{
	public static Task<PipeConnection> ConnectOutAsync<TMessage>(this IPipeConnector pipeConnector,
		Func<IServiceProvider, IHandler<TMessage>> factory, string routingKey = "",
		string subscriptionId = "", CancellationToken token = default) =>
		pipeConnector.ConnectOutAsync<TMessage>(new HandlingPipe<TMessage>(factory), routingKey, subscriptionId, token);

	public static Task<PipeConnection> ConnectOutAsync<TMessage, TResult>(this IPipeConnector pipeConnector,
		Func<IServiceProvider, IHandler<TMessage, TResult>> factory, string routingKey = "",
		CancellationToken token = default) =>
		pipeConnector.ConnectOutAsync<TMessage, TResult>(new HandlingPipe<TMessage, TResult>(factory), routingKey, token);

	public static Task<PipeConnection> ConnectOutAsync<TMessage>(this IPipeConnector pipeConnector,
		IHandler<TMessage> handler, string routingKey = "", string subscriptionId = "",
		CancellationToken token = default) =>
		pipeConnector.ConnectOutAsync(_ => handler, routingKey, subscriptionId, token: token);

	public static Task<PipeConnection> ConnectOutAsync<TMessage, TResult>(this IPipeConnector pipeConnector,
		IHandler<TMessage, TResult> handler, string routingKey = "",
		CancellationToken token = default) =>
		pipeConnector.ConnectOutAsync(_ => handler, routingKey, token: token);

	public static Task<PipeConnection> ConnectOutAsync<TMessage, THandler>(this IPipeConnector pipeConnector,
		string routingKey = "", string subscriptionId = "", CancellationToken token = default)
		where THandler : IHandler<TMessage> =>
		pipeConnector.ConnectOutAsync(p => p.GetRequiredService<THandler>(), routingKey, subscriptionId, token: token);

	public static Task<PipeConnection> ConnectOutAsync<TMessage, TResult, THandler>(this IPipeConnector pipeConnector,
		string routingKey = "", CancellationToken token = default)
		where THandler : IHandler<TMessage, TResult> =>
		pipeConnector.ConnectOutAsync(p => p.GetRequiredService<THandler>(), routingKey, token: token);
}