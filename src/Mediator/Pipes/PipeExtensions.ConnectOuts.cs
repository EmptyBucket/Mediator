using System.Reflection;

namespace Mediator.Pipes;

public static partial class PipeExtensions
{
    /// <summary>
    /// Connect out of this pipe to <paramref name="pipe"/> with <paramref name="messagePredicate"/> and
    /// <paramref name="routingKey"/> routing
    /// </summary>
    /// <param name="pipeConnector"></param>
    /// <param name="pipe"></param>
    /// <param name="assembly"></param>
    /// <param name="messagePredicate"></param>
    /// <param name="routingKey"></param>
    /// <returns></returns>
    public static void ConnectOuts(this IPubPipeConnector pipeConnector, IPubPipe pipe, Assembly assembly,
        Func<Type, bool> messagePredicate, string routingKey = "")
    {
        foreach (var type in assembly.GetTypes().Where(messagePredicate))
            ConnectOut(pipeConnector, type, pipe, routingKey);
    }

    /// <summary>
    /// Connect out of this pipe to <paramref name="pipe"/> with <paramref name="messagePredicate"/>,
    /// <typeparamref name="TResult"/> and <paramref name="routingKey"/> routing
    /// </summary>
    /// <param name="pipeConnector"></param>
    /// <param name="pipe"></param>
    /// <param name="assembly"></param>
    /// <param name="messagePredicate"></param>
    /// <param name="routingKey"></param>
    /// <returns></returns>
    public static void ConnectOuts<TResult>(this IReqPipeConnector pipeConnector, IReqPipe pipe, Assembly assembly,
        Func<Type, bool> messagePredicate, string routingKey = "")
    {
        foreach (var type in assembly.GetTypes().Where(messagePredicate))
            pipeConnector.ConnectOut<TResult>(type, pipe, routingKey);
    }

    private static void ConnectOut(this IPubPipeConnector pipeConnector, Type messageType, IPubPipe pipe,
        string routingKey)
    {
        var genericTypes = new[] { messageType };
        var args = new object[] { pipe, routingKey, string.Empty, string.Empty };
        var argTypes = args.Select(a => a.GetType()).ToArray();
        var connectOutMethod = pipeConnector.GetType()
            .GetMethod(nameof(IPubPipeConnector.ConnectOut), genericTypes.Length, argTypes)
            .MakeGenericMethod(genericTypes);
        connectOutMethod.Invoke(pipeConnector, args);
    }

    private static void ConnectOut<TResult>(this IReqPipeConnector pipeConnector, Type messageType, IReqPipe pipe,
        string routingKey)
    {
        var genericTypes = new[] { messageType, typeof(TResult) };
        var args = new object[] { pipe, routingKey, string.Empty };
        var argTypes = args.Select(a => a.GetType()).ToArray();
        var connectOutMethod = pipeConnector.GetType()
            .GetMethod(nameof(IReqPipeConnector.ConnectOut), genericTypes.Length, argTypes)
            .MakeGenericMethod(genericTypes);
        connectOutMethod.Invoke(pipeConnector, args);
    }
}