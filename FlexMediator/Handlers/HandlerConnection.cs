using FlexMediator.Utils;

namespace FlexMediator.Handlers;

public record HandlerConnection : IDisposable
{
    private readonly Action<HandlerConnection> _disconnect;

    public HandlerConnection(Action<HandlerConnection> disconnect, Route route,
        Func<IServiceProvider, IHandler> factory)
    {
        _disconnect = disconnect;
        Route = route;
        Factory = factory;
    }

    public Route Route { get; }

    public Func<IServiceProvider, IHandler> Factory { get; }

    public void Dispose() => _disconnect(this);
}