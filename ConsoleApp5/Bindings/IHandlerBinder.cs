namespace ConsoleApp5.Bindings;

public interface IHandlerBinder
{
    void Bind<TMessage>(IHandler<TMessage> handler, string routingKey = "");

    void Bind<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>;

    void Unbind<TMessage>(IHandler<TMessage> handler, string routingKey = "");

    void Unbind<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>;
}