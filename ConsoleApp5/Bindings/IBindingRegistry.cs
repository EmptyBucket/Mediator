namespace ConsoleApp5.Bindings;

public interface IBindingRegistry
{
    Task Add<TMessage>(IHandler<TMessage> handler, string routingKey = "");

    Task Add<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "");

    Task Add<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>;

    Task Add<TMessage, THandler, TResult>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>;

    Task Remove<TMessage>(IHandler<TMessage> handler, string routingKey = "");

    Task Remove<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>;
}