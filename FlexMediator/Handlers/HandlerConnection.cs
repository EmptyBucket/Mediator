using FlexMediator.Utils;

namespace FlexMediator.Handlers;

public readonly record struct HandlerConnection : IDisposable
{
    private readonly Action<HandlerConnection> _disconnect;

    public HandlerConnection(Route route, Func<IServiceProvider, IHandler> factory,
        Action<HandlerConnection> disconnect)
    {
        _disconnect = disconnect;
        Route = route;
        Factory = factory;
    }

    public Route Route { get; }

    public Func<IServiceProvider, IHandler> Factory { get; }

    public void Dispose() => _disconnect(this);
}