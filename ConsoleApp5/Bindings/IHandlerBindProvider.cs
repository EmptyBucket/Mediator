namespace ConsoleApp5.Bindings;

public interface IHandlerBindProvider
{
    IEnumerable<HandlerBind> GetBindings<TMessage>(string routingKey = "");

    IEnumerable<HandlerBind> GetBindings<TMessage, TResult>(string routingKey = "");
}