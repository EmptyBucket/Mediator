namespace ConsoleApp5.Topologies;

public interface ITopology
{
    Task BindDispatch<TMessage>(string routingKey = "");
    
    Task BindDispatch<TMessage, TResult>(string routingKey = "");

    Task UnbindDispatch<TMessage>(string routingKey = "");

    Task UnbindDispatch<TMessage, TResult>(string routingKey = "");

    Task BindReceive<TMessage>(IHandler<TMessage> handler, string routingKey = "");

    Task BindReceive<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "");

    Task BindReceive<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>;

    Task BindReceive<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>;

    Task UnbindReceive<TMessage>(IHandler<TMessage> handler, string routingKey = "");

    Task UnbindReceive<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "");

    Task UnbindReceive<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>;

    Task UnbindReceive<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>;
}