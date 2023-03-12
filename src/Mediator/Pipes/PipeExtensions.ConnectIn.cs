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

namespace Mediator.Pipes;

public static partial class PipeExtensions
{
    /// <summary>
    /// Connect in of this pipe to <paramref name="pipeConnector"/>
    /// </summary>
    /// <param name="pipe"></param>
    /// <param name="pipeConnector"></param>
    /// <param name="routingKey"></param>
    /// <param name="subscriptionId"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <returns></returns>
    public static IDisposable ConnectIn<TMessage>(this IPubPipe pipe, IPubPipeConnector pipeConnector,
        string routingKey = "", string subscriptionId = "") =>
        pipeConnector.ConnectOut<TMessage>(pipe, routingKey, subscriptionId);

    /// <summary>
    /// Connect in of this pipe to <paramref name="pipeConnector"/>
    /// </summary>
    /// <param name="pipe"></param>
    /// <param name="pipeConnector"></param>
    /// <param name="routingKey"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static IDisposable ConnectIn<TMessage, TResult>(this IReqPipe pipe, IReqPipeConnector pipeConnector,
        string routingKey = "") =>
        pipeConnector.ConnectOut<TMessage, TResult>(pipe, routingKey);
}