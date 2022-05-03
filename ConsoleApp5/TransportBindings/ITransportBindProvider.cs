namespace ConsoleApp5.TransportBindings;

public interface ITransportBindProvider
{
    IEnumerable<TransportBind> GetBinds<TMessage>(string routingKey = "");
}