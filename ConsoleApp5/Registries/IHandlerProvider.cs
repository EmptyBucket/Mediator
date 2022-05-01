namespace ConsoleApp5.Registries;

public interface IHandlerProvider
{
    IReadOnlyCollection<IHandler> GetHandlers<TMessage>(string? routingKey = null);
}