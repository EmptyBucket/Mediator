namespace ConsoleApp5.TransportBindings;

public interface ITransportSubscriber
{
    Task Subscribe<TMessage>(string routingKey = "");
    
    Task Subscribe<TMessage, TResult>(string routingKey = "");

    Task Unsubscribe<TMessage>(string routingKey = "");
}