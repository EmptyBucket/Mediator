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

using System.Reflection;

namespace Mediator.Pipes;

public static partial class PipeExtensions
{
    /// <summary>
    /// Connect out to <paramref name="pipe"/> with <paramref name="messagePredicate"/> and
    /// <paramref name="routingKey"/> routing
    /// </summary>
    /// <param name="connector"></param>
    /// <param name="pipe"></param>
    /// <param name="assembly"></param>
    /// <param name="messagePredicate"></param>
    /// <param name="routingKey"></param>
    /// <returns></returns>
    public static void ConnectOuts(this IMulticastPubConnector connector, IPubPipe pipe, Assembly assembly,
        Func<Type, bool> messagePredicate, string routingKey = "")
    {
        foreach (var type in assembly.GetTypes().Where(messagePredicate))
            ConnectOut(connector, type, pipe, routingKey);
    }

    /// <summary>
    /// Connect out to <paramref name="pipe"/> with <paramref name="messagePredicate"/>,
    /// <typeparamref name="TResult"/> and <paramref name="routingKey"/> routing
    /// </summary>
    /// <param name="connector"></param>
    /// <param name="pipe"></param>
    /// <param name="assembly"></param>
    /// <param name="messagePredicate"></param>
    /// <param name="routingKey"></param>
    /// <returns></returns>
    public static void ConnectOuts<TResult>(this IMulticastReqConnector connector, IReqPipe pipe, Assembly assembly,
        Func<Type, bool> messagePredicate, string routingKey = "")
    {
        foreach (var type in assembly.GetTypes().Where(messagePredicate))
            connector.ConnectOut<TResult>(type, pipe, routingKey);
    }

    private static void ConnectOut(this IMulticastPubConnector connector, Type messageType, IPubPipe pipe,
        string routingKey)
    {
        var genericTypes = new[] { messageType };
        var args = new object[] { pipe, routingKey, string.Empty, string.Empty };
        var argTypes = args.Select(a => a.GetType()).ToArray();
        var connectOutMethod = connector.GetType()
            .GetMethod(nameof(IMulticastPubConnector.ConnectOut), genericTypes.Length, argTypes)
            .MakeGenericMethod(genericTypes);
        connectOutMethod.Invoke(connector, args);
    }

    private static void ConnectOut<TResult>(this IMulticastReqConnector connector, Type messageType, IReqPipe pipe,
        string routingKey)
    {
        var genericTypes = new[] { messageType, typeof(TResult) };
        var args = new object[] { pipe, routingKey, string.Empty };
        var argTypes = args.Select(a => a.GetType()).ToArray();
        var connectOutMethod = connector.GetType()
            .GetMethod(nameof(IMulticastReqConnector.ConnectOut), genericTypes.Length, argTypes)
            .MakeGenericMethod(genericTypes);
        connectOutMethod.Invoke(connector, args);
    }
}