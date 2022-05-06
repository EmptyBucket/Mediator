namespace FlexMediator.Handlers;

public interface IHandleConnector
{
    HandlerConnection Connect<TMessage>(Func<IServiceProvider, IHandler> factory, string routingKey = "");

    HandlerConnection Connect<TMessage, TResult>(Func<IServiceProvider, IHandler> factory, string routingKey = "");
}