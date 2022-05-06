using FlexMediator.Handlers;
using FlexMediator.Pipes;

namespace FlexMediator.Topologies;

public readonly record struct TopologyBind : IAsyncDisposable
{
    private readonly Func<TopologyBind, ValueTask> _unbind;

    public TopologyBind(Func<TopologyBind, ValueTask> unbind, Route route, PipeBind pipeBind,
        HandlerBind? handlerBind = null)
    {
        _unbind = unbind;
        Route = route;
        PipeBind = pipeBind;
        HandlerBind = handlerBind;
    }

    public Route Route { get; }

    public PipeBind PipeBind { get; }

    public HandlerBind? HandlerBind { get; }

    public ValueTask DisposeAsync() => _unbind(this);
}