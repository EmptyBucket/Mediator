using FlexMediator.Handlers;
using FlexMediator.Pipes;

namespace FlexMediator.Plumbers;

public interface IPlumber
{
    Task<IPlumber> Connect<TMessage, TPipe>(string routingKey = "",
        Action<IPlumber<TMessage>>? next = null)
        where TPipe : IPipe, IPipeConnector;

    Task<IPlumber> Connect<TMessage, TResult, TPipe>(string routingKey = "",
        Action<IPlumber<TMessage, TResult>>? next = null)
        where TPipe : IPipe, IPipeConnector;

    Task<IPlumber> ConnectHandlers<TMessage>(string routingKey = "",
        Action<IHandlerPlumber<TMessage>>? next = null);

    Task<IPlumber> ConnectHandlers<TMessage, TResult>(string routingKey = "",
        Action<IHandlerPlumber<TMessage, TResult>>? next = null);
}

public interface IPlumber<TMessage>
{
    Task<IPlumber<TMessage>> Connect<TPipe>(string routingKey = "",
        Action<IPlumber<TMessage>>? next = null)
        where TPipe : IPipe, IPipeConnector;

    Task<IPlumber<TMessage>> ConnectHandlers(string routingKey = "",
        Action<IHandlerPlumber<TMessage>>? next = null);
}

public interface IPlumber<TMessage, TResult>
{
    Task<IPlumber<TMessage, TResult>> Connect<TPipe>(string routingKey = "",
        Action<IPlumber<TMessage, TResult>>? next = null)
        where TPipe : IPipe, IPipeConnector;

    Task<IPlumber<TMessage, TResult>> ConnectHandlers(string routingKey = "",
        Action<IHandlerPlumber<TMessage, TResult>>? next = null);
}

public interface IHandlerPlumber<TMessage>
{
    IHandlerPlumber<TMessage> Connect(Func<IServiceProvider, IHandler> factory, string routingKey = "");
}

public interface IHandlerPlumber<TMessage, TResult>
{
    IHandlerPlumber<TMessage, TResult> Connect(Func<IServiceProvider, IHandler> factory, string routingKey = "");
}