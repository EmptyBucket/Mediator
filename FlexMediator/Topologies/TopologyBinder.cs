using FlexMediator.Handlers;
using FlexMediator.Pipes;

namespace FlexMediator.Topologies;

public class TopologyBinder : ITopologyBinder
{
    private readonly IPipe _boundDispatchPipe;
    private readonly IPipe _boundReceivePipe;
    private readonly IPipeBinder _dispatchPipeBinder;
    private readonly IPipeBinder _receivePipeBinder;
    private readonly IHandlerBinder _handlerBinder;

    public TopologyBinder(IPipe boundDispatchPipe, IPipe boundReceivePipe,
        IPipeBinder dispatchPipeBinder, IPipeBinder receivePipeBinder,
        IHandlerBinder handlerBinder)
    {
        _boundDispatchPipe = boundDispatchPipe;
        _boundReceivePipe = boundReceivePipe;
        _dispatchPipeBinder = dispatchPipeBinder;
        _receivePipeBinder = receivePipeBinder;
        _handlerBinder = handlerBinder;
    }

    public async Task<TopologyBind> BindDispatch<TMessage>(string routingKey = "")
    {
        var pipeBind = await _dispatchPipeBinder.Bind<TMessage>(_boundDispatchPipe, routingKey);
        var route = new Route(typeof(TMessage), routingKey);
        return new TopologyBind(Unbind, route, pipeBind);
    }

    public async Task<TopologyBind> BindDispatch<TMessage, TResult>(string routingKey = "")
    {
        var pipeBind = await _dispatchPipeBinder.Bind<TMessage, TResult>(_boundDispatchPipe, routingKey);
        var route = new Route(typeof(TMessage), routingKey, typeof(TResult));
        return new TopologyBind(Unbind, route, pipeBind);
    }

    public async Task<TopologyBind> BindReceive<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var pipeBind = await _receivePipeBinder.Bind<TMessage>(_boundReceivePipe);
        var handlerBind = _handlerBinder.Bind<TMessage, THandler>(routingKey);
        var route = new Route(typeof(TMessage), routingKey);
        return new TopologyBind(Unbind, route, pipeBind, handlerBind);
    }

    public async Task<TopologyBind> BindReceive<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>
    {
        var pipeBind = await _receivePipeBinder.Bind<TMessage, TResult>(_boundReceivePipe);
        var handlerBind = _handlerBinder.Bind<TMessage, TResult, THandler>(routingKey);
        var route = new Route(typeof(TMessage), routingKey, typeof(TResult));
        return new TopologyBind(Unbind, route, pipeBind, handlerBind);
    }

    public async Task<TopologyBind> BindReceive<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var pipeBind = await _receivePipeBinder.Bind<TMessage>(_boundReceivePipe);
        var handlerBind = _handlerBinder.Bind(handler, routingKey);
        var route = new Route(typeof(TMessage), routingKey);
        return new TopologyBind(Unbind, route, pipeBind, handlerBind);
    }

    public async Task<TopologyBind> BindReceive<TMessage, TResult>(IHandler<TMessage, TResult> handler,
        string routingKey = "")
    {
        var pipeBind = await _receivePipeBinder.Bind<TMessage, TResult>(_boundReceivePipe);
        var handlerBind = _handlerBinder.Bind(handler, routingKey);
        var route = new Route(typeof(TMessage), routingKey, typeof(TResult));
        return new TopologyBind(Unbind, route, pipeBind, handlerBind);
    }

    private static async ValueTask Unbind(TopologyBind topologyBind)
    {
        topologyBind.HandlerBind?.Dispose();
        await topologyBind.PipeBind.DisposeAsync();
    }
}