namespace FlexMediator.Pipes;

public static class PipeExtensions
{
    public static Task<PipeConnection> Into<TMessage>(this IPipeConnector pipeConnector,
        Func<IServiceProvider, IHandler<TMessage>> factory, string routingKey = "") =>
        pipeConnector.Into<TMessage>(new HandlerPipe<TMessage>(factory), routingKey);

    public static Task<PipeConnection> Into<TMessage, TResult>(this IPipeConnector pipeConnector, IPipe pipe,
        string routingKey = "") =>
        pipeConnector.Into<TMessage, TResult>(pipe, routingKey);

    public static Task<PipeConnection> From<TMessage>(this IPipe pipe, IPipeConnector pipeConnector,
        string routingKey = "") =>
        pipeConnector.Into<TMessage>(pipe, routingKey);

    public static Task<PipeConnection> From<TMessage, TResult>(this IPipe pipe, IPipeConnector pipeConnector,
        string routingKey = "") =>
        pipeConnector.Into<TMessage, TResult>(pipe, routingKey);
}