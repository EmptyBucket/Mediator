namespace FlexMediator.Pipes;

public readonly record struct PipeBind : IAsyncDisposable
{
    private readonly Func<PipeBind, ValueTask> _unbind;

    public PipeBind(Func<PipeBind, ValueTask> unbind, Route route, IPipe pipe)
    {
        _unbind = unbind;
        Route = route;
        Pipe = pipe;
    }

    public Route Route { get; }

    public IPipe Pipe { get; }

    public ValueTask DisposeAsync() => _unbind(this);
}