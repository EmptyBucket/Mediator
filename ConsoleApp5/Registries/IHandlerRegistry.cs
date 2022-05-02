namespace ConsoleApp5.Registries;

public interface IHandlerRegistry
{
    Task AddHandler<TMessage>(IHandler<TMessage> handler, string routingKey = "");

    Task AddHandler<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "");

    Task AddHandler<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>;

    Task AddHandler<TMessage, THandler, TResult>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>;

    Task RemoveHandler<TMessage>(IHandler<TMessage> handler, string routingKey = "");

    Task RemoveHandler<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>;
}

