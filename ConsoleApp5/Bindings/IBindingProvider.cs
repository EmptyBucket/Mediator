namespace ConsoleApp5.Bindings;

public interface IBindingProvider
{
    IEnumerable<Binding> Get<TMessage>(string routingKey = "");
}