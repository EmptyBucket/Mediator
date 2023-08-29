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
    /// <param name="predicate"></param>
    /// <returns></returns>
    public static void ConnectHandlers(this IPipeConnector pipeConnector, Assembly assembly,
        string routingKey = "", Func<Type, bool>? predicate = null)
    {
        ((IPubPipeConnector)pipeConnector).ConnectHandlers(assembly, routingKey, predicate);
        ((IReqPipeConnector)pipeConnector).ConnectHandlers(assembly, routingKey, predicate);
    }

    /// <summary>
    /// Connect the handlers that the ServiceProvider builds for publish/subscribe messaging model
    /// </summary>
    /// <param name="pipeConnector"></param>
    /// <param name="assembly"></param>
    /// <param name="routingKey"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public static void ConnectHandlers(this IPubPipeConnector pipeConnector, Assembly assembly,
        string routingKey = "", Func<Type, bool>? predicate = null)
    {
        predicate ??= _ => true;
        var handlerInterface = typeof(IHandler<>);

        foreach (var type in assembly.GetTypes().Where(predicate))
        foreach (var @interface in type.GetInterfaces()
                     .Where(i => i.IsGenericType && handlerInterface == i.GetGenericTypeDefinition()))
        {
            var args = new object[] { pipeConnector, routingKey, string.Empty, string.Empty };
            var genericTypes = @interface.GetGenericArguments().Append(type).ToArray();
            var connectHandlerMethod = GetConnectHandlerMethod(args, genericTypes);
            connectHandlerMethod.Invoke(null, args);
        }
    }

    /// <summary>
    /// Connect the handlers that the ServiceProvider builds for request/response messaging model
    /// </summary>
    /// <param name="pipeConnector"></param>
    /// <param name="assembly"></param>
    /// <param name="routingKey"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public static void ConnectHandlers(this IReqPipeConnector pipeConnector, Assembly assembly,
        string routingKey = "", Func<Type, bool>? predicate = null)
    {
        predicate ??= _ => true;
        var handlerInterface = typeof(IHandler<,>);

        foreach (var type in assembly.GetTypes().Where(predicate))
        foreach (var @interface in type.GetInterfaces()
                     .Where(i => i.IsGenericType && handlerInterface == i.GetGenericTypeDefinition()))
        {
            var args = new object[] { pipeConnector, routingKey, string.Empty };
            var genericTypes = @interface.GetGenericArguments().Append(type).ToArray();
            var connectHandlerMethod = GetConnectHandlerMethod(args, genericTypes);
            connectHandlerMethod.Invoke(null, args);
        }
    }

    private static MethodInfo GetConnectHandlerMethod(object[] args, Type[] genericTypes)
    {
        var argTypes = args.Select(a => a.GetType()).ToArray();
        var connectHandlerMethod = typeof(PipeExtensions)
            .GetMethod(nameof(ConnectHandler), genericTypes.Length, BindingFlags.Static | BindingFlags.Public, null,
                argTypes, Array.Empty<ParameterModifier>())
            .MakeGenericMethod(genericTypes);
        return connectHandlerMethod;
    }
}