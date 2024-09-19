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

using Mediator.Pipes.Utils;

namespace Mediator.Pipes;

/// <summary>
/// Represents the connector interface for connecting to other pipes for request/response messaging model
/// </summary>
public interface IMulticastReqConnector : IAsyncDisposable
{
    /// <summary>
    /// Get connected pipe connections
    /// </summary>
    /// <returns></returns>
    IEnumerable<PipeConnection<IReqPipe>> GetReqConnections();

    /// <summary>
    /// Connect out to <paramref name="pipe"/> with <typeparamref name="TMessage"/>,
    /// <typeparamref name="TResult"/> and <paramref name="routingKey"/> routing and <paramref name="connectionName"/>
    /// </summary>
    /// <param name="pipe"></param>
    /// <param name="routingKey"></param>
    /// <param name="connectionName"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    PipeConnection<IReqPipe> ConnectOut<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        string connectionName = "");

    /// <summary>
    /// Connect out to <paramref name="pipe"/> with <typeparamref name="TMessage"/>,
    /// <typeparamref name="TResult"/> and <paramref name="routingKey"/> routing and <paramref name="connectionName"/>
    /// </summary>
    /// <param name="pipe"></param>
    /// <param name="routingKey"></param>
    /// <param name="connectionName"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    Task<PipeConnection<IReqPipe>> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        string connectionName = "", CancellationToken cancellationToken = default);
}