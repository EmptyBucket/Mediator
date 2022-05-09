namespace FlexMediator.Pipes;

public static class PipeExtensions
{
    public static Task<PipeConnection> ConnectOutAsync<TMessage>(this IPipeConnector pipeConnector,
        Func<IServiceProvider, IHandler<TMessage>> factory, string routingKey = "",
        string subscriptionName = "") =>
        pipeConnector.ConnectOutAsync<TMessage>(new HandlingPipe<TMessage>(factory), routingKey, 
            subscriptionName);

    public static Task<PipeConnection> ConnectOutAsync<TMessage, TResult>(this IPipeConnector pipeConnector,
        Func<IServiceProvider, IHandler<TMessage, TResult>> factory, string routingKey = "",
        string subscriptionName = "") =>
        pipeConnector.ConnectOutAsync<TMessage, TResult>(new HandlingPipe<TMessage, TResult>(factory), routingKey);

    public static Task<PipeConnection> ConnectInAsync<TMessage>(this IPipe pipe, IPipeConnector pipeConnector,
        string routingKey = "", string subscriptionName = "") =>
        pipeConnector.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionName);

    public static Task<PipeConnection> ConnectInAsync<TMessage, TResult>(this IPipe pipe, IPipeConnector pipeConnector,
        string routingKey = "", string subscriptionName = "") =>
        pipeConnector.ConnectOutAsync<TMessage, TResult>(pipe, routingKey);
}