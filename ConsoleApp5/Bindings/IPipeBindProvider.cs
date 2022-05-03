namespace ConsoleApp5.Bindings;

public interface IPipeBindProvider
{
    IEnumerable<PipeBind> GetBindings<TMessage>(string routingKey = "");
}