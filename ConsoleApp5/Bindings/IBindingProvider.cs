namespace ConsoleApp5.Bindings;

public interface IBindingProvider
{
    IEnumerable<Binding<TMessage>> Get<TMessage>(string routingKey = "");
}