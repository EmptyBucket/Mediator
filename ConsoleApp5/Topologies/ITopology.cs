namespace ConsoleApp5.Topologies;

public interface ITopology
{
    public Task BindDispatch<TMessage>(string routingKey = "");

    public Task BindReceive<TMessage>(IHandler<TMessage> handler, string routingKey = "");

    public Task BindReceive<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>;

    public Task BindReceive<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "");

    public Task BindReceive<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>;

    Task UnbindDispatch<TMessage>(string routingKey = "");

    Task UnbindReceive<TMessage>(IHandler<TMessage> handler, string routingKey = "");

    Task UnbindReceive<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>;
}