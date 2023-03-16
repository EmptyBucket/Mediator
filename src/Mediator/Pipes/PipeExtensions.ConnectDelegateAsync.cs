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

namespace Mediator.Pipes;

public static partial class PipeExtensions
{
    /// <summary>
    /// Connect handler with <paramref name="func"/> delegate for publish/subscribe messaging model
    /// </summary>
    /// <param name="pipeConnector"></param>
    /// <param name="func"></param>
    /// <param name="routingKey"></param>
    /// <param name="connectionName"></param>
    /// <param name="subscriptionId"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <returns></returns>
    public static Task<PipeConnection<IPubPipe>> ConnectDelegateAsync<TMessage>(this IPubPipeConnector pipeConnector,
        Func<MessageContext<TMessage>, CancellationToken, Task> func, string routingKey = "",
        string connectionName = "", string subscriptionId = "", CancellationToken cancellationToken = default) =>
        pipeConnector.ConnectHandlerAsync(new LambdaHandler<TMessage>(func), routingKey, connectionName, subscriptionId,
            cancellationToken);

    /// <summary>
    /// Connect handler with <paramref name="func"/> delegate for request/response messaging model
    /// </summary>
    /// <param name="pipeConnector"></param>
    /// <param name="func"></param>
    /// <param name="routingKey"></param>
    /// <param name="connectionName"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static Task<PipeConnection<IReqPipe>> ConnectDelegateAsync<TMessage, TResult>(
        this IReqPipeConnector pipeConnector, Func<MessageContext<TMessage>, CancellationToken, Task<TResult>> func,
        string routingKey = "", string connectionName = "", CancellationToken cancellationToken = default) =>
        pipeConnector.ConnectHandlerAsync(new LambdaHandler<TMessage, TResult>(func), routingKey, connectionName,
            cancellationToken);
}