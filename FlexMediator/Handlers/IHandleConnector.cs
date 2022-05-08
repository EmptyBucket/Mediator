namespace FlexMediator.Handlers;

public interface IHandleConnector
{
    HandlerConnection BindHandler<TMessage>(Func<IServiceProvider, IHandler> factory, string routingKey = "");

    HandlerConnection BindHandler<TMessage, TResult>(Func<IServiceProvider, IHandler> factory, string routingKey = "");
}