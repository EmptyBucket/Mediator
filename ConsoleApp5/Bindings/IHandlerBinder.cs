namespace ConsoleApp5.Bindings;

public interface IHandlerBinder
{
    void Bind<TMessage>(IHandler<TMessage> handler, string routingKey = "");

    void Bind<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "");

    void Bind<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>;

    void Bind<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>;

    void Unbind<TMessage>(IHandler<TMessage> handler, string routingKey = "");

    void Unbind<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "");

    void Unbind<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>;

    void Unbind<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>;
}