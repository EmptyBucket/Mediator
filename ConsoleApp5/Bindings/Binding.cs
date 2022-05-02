namespace ConsoleApp5.Bindings;

public readonly record struct Binding
{
    public Binding(Route route, IHandler? handler = null, Type? handlerType = null)
    {
        if (handler is null && handlerType is null)
            throw new ArgumentException($"{nameof(handler)} or {nameof(handlerType)} must be not-null");

        Route = route;
        Handler = handler;
        HandlerType = handlerType;
    }

    public Route Route { get; }

    public IHandler? Handler { get; }

    public Type? HandlerType { get; }
}