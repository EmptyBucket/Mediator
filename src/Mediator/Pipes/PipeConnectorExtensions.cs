// MIT License
// 
// Copyright (c) 2022 Alexey Politov
// https://github.com/EmptyBucket/Mediator
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

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