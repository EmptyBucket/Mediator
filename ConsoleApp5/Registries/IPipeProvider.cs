using ConsoleApp5.Pipes;

namespace ConsoleApp5.Registries;

public interface IPipeProvider
{
    IReadOnlyCollection<IPipe> GetPipes<TMessage>(string? routingKey = null);
}