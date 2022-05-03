namespace ConsoleApp5.Bindings;

public interface IHandlerBindProvider
{
    IEnumerable<HandlerBind> GetBindings<TMessage>(string routingKey = "");
}