namespace FlexMediator.Handlers;

public readonly record struct HandlerBind : IDisposable
{
    private readonly Action<HandlerBind> _unbind;

    public HandlerBind(Action<HandlerBind> unbind, Route route, Type? handlerType = null, IHandler? handler = null)
    {
        if (handlerType is null && handler is null)
            throw new ArgumentException($"{nameof(handlerType)} or {nameof(handler)} must be not-null");

        _unbind = unbind;

        Route = route;
        Handler = handler;
        HandlerType = handlerType;
    }

    public Route Route { get; }

    public Type? HandlerType { get; }
    
    public IHandler? Handler { get; }

    public void Dispose() => _unbind(this);
}