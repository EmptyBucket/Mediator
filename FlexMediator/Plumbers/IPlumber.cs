using FlexMediator.Pipes;

namespace FlexMediator.Plumbers;

public interface IPlumber : IAsyncDisposable
{
    Task<IPlumber> Connect<TMessage, TPipe>(string routingKey = "", Action<IPipeConnector>? make = null)
        where TPipe : IPipe, IPipeConnector;

    Task<IPlumber> Connect<TMessage, TResult, TPipe>(string routingKey = "", Action<IPipeConnector>? make = null)
        where TPipe : IPipe, IPipeConnector;
}