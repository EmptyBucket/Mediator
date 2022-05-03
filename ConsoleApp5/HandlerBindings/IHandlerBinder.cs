using ConsoleApp5.Models;

namespace ConsoleApp5.HandlerBindings;

public interface IHandlerBinder
{
    Task Bind<TMessage>(IHandler<TMessage> handler, string routingKey = "");

    Task Bind<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "");

    Task Bind<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>;

    Task Bind<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>;

    Task Unbind<TMessage>(IHandler<TMessage> handler, string routingKey = "");

    Task Unbind<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>;
}