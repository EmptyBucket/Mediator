namespace ConsoleApp5.Bindings;

public interface IPipeBindProvider
{
    IEnumerable<PipeBind> GetBindings<TMessage>(string routingKey = "");

    IEnumerable<PipeBind> GetBindings<TMessage, TResult>(string routingKey = "");
}