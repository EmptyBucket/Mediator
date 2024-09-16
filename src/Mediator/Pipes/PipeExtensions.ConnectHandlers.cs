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
    /// <param name="routingKey"></param>
    /// <param name="handlerPredicate"></param>
    /// <returns></returns>
    public static void ConnectHandlers(this IPipeConnector pipeConnector, Assembly assembly,
        string routingKey = "", Func<Type, bool>? handlerPredicate = null)
    {
        ((IPubPipeConnector)pipeConnector).ConnectHandlers(assembly, routingKey, handlerPredicate);
        ((IReqPipeConnector)pipeConnector).ConnectHandlers(assembly, routingKey, handlerPredicate);
    }

    /// <summary>
    /// Connect the handlers that the ServiceProvider builds for publish/subscribe messaging model
    /// </summary>
    /// <param name="pipeConnector"></param>
    /// <param name="assembly"></param>
    /// <param name="routingKey"></param>
    /// <param name="handlerPredicate"></param>
    /// <returns></returns>
    public static void ConnectHandlers(this IPubPipeConnector pipeConnector, Assembly assembly,
        string routingKey = "", Func<Type, bool>? handlerPredicate = null)
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
    /// <param name="routingKey"></param>
    /// <param name="handlerPredicate"></param>
    /// <returns></returns>
    public static void ConnectHandlers(this IReqPipeConnector pipeConnector, Assembly assembly,
        string routingKey = "", Func<Type, bool>? handlerPredicate = null)
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