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
using Mediator.Pipes.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Pipes;

public static partial class PipeExtensions
{
    /// <summary>
    /// Connect the handler that the <paramref name="factory"/> builds for publish/subscribe messaging model
    /// </summary>
    /// <param name="pipeConnector"></param>
    /// <param name="factory"></param>
    /// <param name="routingKey"></param>
    /// <param name="connectionName"></param>
    /// <param name="subscriptionId"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <returns></returns>
    public static PipeConnection<IPubPipe> ConnectHandler<TMessage>(this IPubPipeConnector pipeConnector,
        Func<IServiceProvider, IHandler<TMessage>> factory, string routingKey = "", string connectionName = "",
        string subscriptionId = "") =>
        pipeConnector.ConnectOut<TMessage>(new HandlingPipe<TMessage>(factory), routingKey, connectionName,
            subscriptionId);

    /// <summary>
    /// Connect the handler for publish/subscribe messaging model
    /// </summary>
    /// <param name="pipeConnector"></param>
    /// <param name="handler"></param>
    /// <param name="routingKey"></param>
    /// <param name="connectionName"></param>
    /// <param name="subscriptionId"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <returns></returns>
    public static PipeConnection<IPubPipe> ConnectHandler<TMessage>(this IPubPipeConnector pipeConnector,
        IHandler<TMessage> handler, string routingKey = "", string connectionName = "", string subscriptionId = "") =>
        pipeConnector.ConnectHandler(_ => handler, routingKey, connectionName, subscriptionId);

    /// <summary>
    /// Connect the handler that the ServiceProvider builds for publish/subscribe messaging model
    /// </summary>
    /// <param name="pipeConnector"></param>
    /// <param name="routingKey"></param>
    /// <param name="connectionName"></param>
    /// <param name="subscriptionId"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="THandler"></typeparam>
    /// <returns></returns>
    public static PipeConnection<IPubPipe> ConnectHandler<TMessage, THandler>(this IPubPipeConnector pipeConnector,
        string routingKey = "", string connectionName = "", string subscriptionId = "")
        where THandler : IHandler<TMessage> =>
        pipeConnector.ConnectHandler(p => p.GetRequiredService<THandler>(), routingKey, connectionName, subscriptionId);

    /// <summary>
    /// Connect the handler that the <paramref name="factory"/> builds for request/response messaging model
    /// </summary>
    /// <param name="pipeConnector"></param>
    /// <param name="factory"></param>
    /// <param name="routingKey"></param>
    /// <param name="connectionName"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static PipeConnection<IReqPipe> ConnectHandler<TMessage, TResult>(this IReqPipeConnector pipeConnector,
        Func<IServiceProvider, IHandler<TMessage, TResult>> factory, string routingKey = "",
        string connectionName = "") =>
        pipeConnector.ConnectOut<TMessage, TResult>(new HandlingPipe<TMessage, TResult>(factory), routingKey,
            connectionName);

    /// <summary>
    /// Connect the handler for request/response messaging model
    /// </summary>
    /// <param name="pipeConnector"></param>
    /// <param name="handler"></param>
    /// <param name="routingKey"></param>
    /// <param name="connectionName"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static PipeConnection<IReqPipe> ConnectHandler<TMessage, TResult>(this IReqPipeConnector pipeConnector,
        IHandler<TMessage, TResult> handler, string routingKey = "", string connectionName = "") =>
        pipeConnector.ConnectHandler(_ => handler, routingKey, connectionName);

    /// <summary>
    /// Connect the handler that the ServiceProvider builds for request/response messaging model
    /// </summary>
    /// <param name="pipeConnector"></param>
    /// <param name="routingKey"></param>
    /// <param name="connectionName"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="THandler"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static PipeConnection<IReqPipe> ConnectHandler<TMessage, TResult, THandler>(
        this IReqPipeConnector pipeConnector, string routingKey = "", string connectionName = "")
        where THandler : IHandler<TMessage, TResult> =>
        pipeConnector.ConnectHandler(p => p.GetRequiredService<THandler>(), routingKey, connectionName);
}