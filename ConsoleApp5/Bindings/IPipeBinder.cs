using ConsoleApp5.Pipes;

namespace ConsoleApp5.Bindings;

public interface IPipeBinder
{
    Task Bind<TMessage>(IPipe pipe, string routingKey = "");
    
    Task Bind<TMessage, TResult>(IPipe pipe, string routingKey = "");
    
    Task Unbind<TMessage>(IPipe pipe, string routingKey = "");
}