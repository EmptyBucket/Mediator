namespace ConsoleApp5.Bindings;

public interface IBindingRegistry
{
    Task AddBinding<TMessage>(IHandler<TMessage> handler, string routingKey = "");

    Task AddBinding<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "");

    Task AddBinding<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>;

    Task AddBinding<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>;

    Task RemoveBinding<TMessage>(IHandler<TMessage> handler, string routingKey = "");

    Task RemoveBinding<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>;
}