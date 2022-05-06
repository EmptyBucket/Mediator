namespace FlexMediator.Handlers;

public interface IHandlerPlumber
{
    HandlerBind Bind<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>;

    HandlerBind Bind<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>;

    HandlerBind Bind<TMessage>(IHandler<TMessage> handler, string routingKey = "");

    HandlerBind Bind<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "");
}