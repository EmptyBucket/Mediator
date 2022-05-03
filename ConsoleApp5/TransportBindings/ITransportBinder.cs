using ConsoleApp5.Models;

namespace ConsoleApp5.TransportBindings;

public interface ITransportBinder
{
    Task Bind<TMessage>(Transport transport, string routingKey = "");
    
    Task Unbind<TMessage>(Transport transport, string routingKey = "");
}