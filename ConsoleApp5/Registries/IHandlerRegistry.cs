namespace ConsoleApp5.Registries;

public interface IHandlerRegistry
{
    void AddHandler<TMessage>(IHandler handler, string? routingKey = null);

    void AddHandler<TMessage, THandler>(string? routingKey = null) where THandler : IHandler;

    void RemoveHandler<TMessage>(IHandler handler, string? routingKey = null);

    void RemoveHandler<TMessage, THandler>(string? routingKey = null) where THandler : IHandler;
}