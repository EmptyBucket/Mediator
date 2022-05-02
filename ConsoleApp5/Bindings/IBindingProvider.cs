using ConsoleApp5.Models;

namespace ConsoleApp5.Bindings;

public interface IBindingProvider
{
    IEnumerable<Binding> GetBindings<TMessage>(string routingKey = "");
}