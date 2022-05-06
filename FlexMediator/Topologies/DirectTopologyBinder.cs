using FlexMediator.Handlers;
using FlexMediator.Pipes;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediator.Topologies;

public class DirectTopologyBinder : ITopologyBinder
{
    private readonly HandlingPipe _receivePipe;
    private readonly BranchingPipe _dispatchPipe;

    public DirectTopologyBinder(BranchingPipe dispatchPipe, IServiceScopeFactory serviceScopeFactory)
    {
        _dispatchPipe = dispatchPipe;
        _receivePipe = new HandlingPipe(serviceScopeFactory);
    }

    public async Task<TopologyBind> BindDispatch<TMessage>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);
        var pipeBind = await _dispatchPipe.Bind<TMessage>(_receivePipe, routingKey);
        return new TopologyBind(Unbind, route, pipeBind);
    }

    public async Task<TopologyBind> BindDispatch<TMessage, TResult>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey, typeof(TResult));
        var pipeBind = await _dispatchPipe.Bind<TMessage, TResult>(_receivePipe, routingKey);
        return new TopologyBind(Unbind, route, pipeBind);
    }

    public async Task<TopologyBind> BindReceive<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var route = new Route(typeof(TMessage), routingKey);
        var handlerBind = _receivePipe.Bind<TMessage, THandler>(routingKey);
        return new TopologyBind(Unbind, route, pipeBind, handlerBind);
    }

    public async Task<TopologyBind> BindReceive<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>
    {
        var route = new Route(typeof(TMessage), routingKey, typeof(TResult));
        var pipeBind = await _dispatchPipe.Bind<TMessage, TResult>(_receivePipe);
        var handlerBind = _receivePipe.Bind<TMessage, TResult, THandler>(routingKey);
        return new TopologyBind(Unbind, route, pipeBind, handlerBind);
    }

    public async Task<TopologyBind> BindReceive<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);
        var pipeBind = await _dispatchPipe.Bind<TMessage>(_receivePipe);
        var handlerBind = _receivePipe.Bind(handler, routingKey);
        return new TopologyBind(Unbind, route, pipeBind, handlerBind);
    }

    public async Task<TopologyBind> BindReceive<TMessage, TResult>(IHandler<TMessage, TResult> handler,
        string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey, typeof(TResult));
        var pipeBind = await _dispatchPipe.Bind<TMessage, TResult>(_receivePipe);
        var handlerBind = _receivePipe.Bind(handler, routingKey);
        return new TopologyBind(Unbind, route, pipeBind, handlerBind);
    }

    private static async ValueTask Unbind(TopologyBind topologyBind)
    {
        topologyBind.HandlerBind?.Dispose();
        await topologyBind.PipeBind.DisposeAsync();
    }
}