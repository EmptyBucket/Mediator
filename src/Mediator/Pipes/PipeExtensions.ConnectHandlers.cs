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
using Mediator.Handlers;

namespace Mediator.Pipes;

public static partial class PipeExtensions
{
    /// <summary>
    /// Connect the handlers that the ServiceProvider builds for publish/subscribe and request/response messaging model
    /// </summary>
    /// <param name="pipeConnector"></param>
    /// <param name="assembly"></param>
    /// <param name="handlerPredicate"></param>
    /// <param name="routingKey"></param>
    /// <returns></returns>
    public static void ConnectHandlers(this IPipeConnector pipeConnector, Assembly assembly,
        Func<Type, bool>? handlerPredicate = null, string routingKey = "")
    {
        ((IPubPipeConnector)pipeConnector).ConnectHandlers(assembly, handlerPredicate, routingKey);
        ((IReqPipeConnector)pipeConnector).ConnectHandlers(assembly, handlerPredicate, routingKey);
    }

    /// <summary>
    /// Connect the handlers that the ServiceProvider builds for publish/subscribe messaging model
    /// </summary>
    /// <param name="pipeConnector"></param>
    /// <param name="assembly"></param>
    /// <param name="handlerPredicate"></param>
    /// <param name="routingKey"></param>
    /// <returns></returns>
    public static void ConnectHandlers(this IPubPipeConnector pipeConnector, Assembly assembly,
        Func<Type, bool>? handlerPredicate = null, string routingKey = "")
    {
        handlerPredicate ??= _ => true;
        var handlerInterface = typeof(IHandler<>);

        foreach (var type in assembly.GetTypes().Where(handlerPredicate))
        foreach (var @interface in type.GetInterfaces()
                     .Where(i => i.IsGenericType && handlerInterface == i.GetGenericTypeDefinition()))
        {
            var messageType = @interface.GetGenericArguments()[0];
            pipeConnector.ConnectHandler(messageType, type, routingKey);
        }
    }

    /// <summary>
    /// Connect the handlers that the ServiceProvider builds for request/response messaging model
    /// </summary>
    /// <param name="pipeConnector"></param>
    /// <param name="assembly"></param>
    /// <param name="handlerPredicate"></param>
    /// <param name="routingKey"></param>
    /// <returns></returns>
    public static void ConnectHandlers(this IReqPipeConnector pipeConnector, Assembly assembly,
        Func<Type, bool>? handlerPredicate = null, string routingKey = "")
    {
        handlerPredicate ??= _ => true;
        var handlerInterface = typeof(IHandler<,>);

        foreach (var type in assembly.GetTypes().Where(handlerPredicate))
        foreach (var @interface in type.GetInterfaces()
                     .Where(i => i.IsGenericType && handlerInterface == i.GetGenericTypeDefinition()))
        {
            var messageType = @interface.GetGenericArguments()[0];
            var resultType = @interface.GetGenericArguments()[1];
            pipeConnector.ConnectHandler(messageType, resultType, type, routingKey);
        }
    }

    private static void ConnectHandler(this IPubPipeConnector pipeConnector, Type messageType, Type handlerType,
        string routingKey)
    {
        var genericTypes = new[] { messageType, handlerType };
        var args = new object[] { pipeConnector, routingKey, string.Empty, string.Empty };
        var argTypes = args.Select(a => a.GetType()).ToArray();
        var connectHandlerMethod = typeof(PipeExtensions)
            .GetMethod(nameof(ConnectHandler), genericTypes.Length, argTypes)
            .MakeGenericMethod(genericTypes);
        connectHandlerMethod.Invoke(null, args);
    }

    private static void ConnectHandler(this IReqPipeConnector pipeConnector, Type messageType, Type resultType,
        Type handlerType, string routingKey)
    {
        var genericTypes = new[] { messageType, resultType, handlerType };
        var args = new object[] { pipeConnector, routingKey, string.Empty };
        var argTypes = args.Select(a => a.GetType()).ToArray();
        var connectHandlerMethod = typeof(PipeExtensions)
            .GetMethod(nameof(ConnectHandler), genericTypes.Length, argTypes)
            .MakeGenericMethod(genericTypes);
        connectHandlerMethod.Invoke(null, args);
    }
}