namespace ConsoleApp5.Registries;

public interface ITopologyProvider
{
    IEnumerable<(string PipeName, IHandler Handler)> GetTopologies<TMessage>(string routingKey = "");
}