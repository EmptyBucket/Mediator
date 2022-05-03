namespace ConsoleApp5.HandlerBindings;

public interface IHandlerBindProvider
{
    IEnumerable<HandlerBind> GetBindings<TMessage>(string routingKey = "");
}