namespace FlexMediator.Handlers;

public interface IHandleConnector
{
    HandlerConnection Out<TMessage>(Func<IServiceProvider, IHandler> factory, string routingKey = "");

    HandlerConnection Out<TMessage, TResult>(Func<IServiceProvider, IHandler> factory, string routingKey = "");
}