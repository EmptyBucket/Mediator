using FlexMediator.Handlers;
using FlexMediator.Pipes;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediator.Topologies;

public class DirectTopologyBinder : ITopologyBinder
{
    private readonly IPipe _boundDispatchPipe;
    private readonly IPipeBinder _dispatchPipeBinder;
    private readonly IHandlerBinder _handlerBinder;

    public DirectTopologyBinder(IPipeBinder dispatchPipeBinder, IServiceScopeFactory serviceScopeFactory)
    {
        var handlerBinder = new HandlerBinder();
        var dispatchReceivePipe = new HandlingPipe(handlerBinder, serviceScopeFactory);
        _boundDispatchPipe = dispatchReceivePipe;
        _dispatchPipeBinder = dispatchPipeBinder;
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
        var pipeBind = await _dispatchPipeBinder.Bind<TMessage>(_boundDispatchPipe);
        var handlerBind = _handlerBinder.Bind<TMessage, THandler>(routingKey);
        var route = new Route(typeof(TMessage), routingKey);
        return new TopologyBind(Unbind, route, pipeBind, handlerBind);
    }

    public async Task<TopologyBind> BindReceive<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>
    {
        var pipeBind = await _dispatchPipeBinder.Bind<TMessage, TResult>(_boundDispatchPipe);
        var handlerBind = _handlerBinder.Bind<TMessage, TResult, THandler>(routingKey);
        var route = new Route(typeof(TMessage), routingKey, typeof(TResult));
        return new TopologyBind(Unbind, route, pipeBind, handlerBind);
    }

    public async Task<TopologyBind> BindReceive<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var pipeBind = await _dispatchPipeBinder.Bind<TMessage>(_boundDispatchPipe);
        var handlerBind = _handlerBinder.Bind(handler, routingKey);
        var route = new Route(typeof(TMessage), routingKey);
        return new TopologyBind(Unbind, route, pipeBind, handlerBind);
    }

    public async Task<TopologyBind> BindReceive<TMessage, TResult>(IHandler<TMessage, TResult> handler,
        string routingKey = "")
    {
        var pipeBind = await _dispatchPipeBinder.Bind<TMessage, TResult>(_boundDispatchPipe);
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