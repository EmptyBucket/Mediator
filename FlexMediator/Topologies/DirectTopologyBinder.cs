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
        var pipeBind = await _dispatchPipe.Bind<TMessage>(_receivePipe, routingKey);
    }

    public async Task<TopologyBind> BindDispatch<TMessage, TResult>(string routingKey = "")
    {
        var pipeBind = await _dispatchPipe.Bind<TMessage, TResult>(_receivePipe, routingKey);
    }

    public async Task<TopologyBind> BindReceive<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var handlerBind = _receivePipe.Bind<TMessage, THandler>(routingKey);
    }

    public async Task<TopologyBind> BindReceive<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>
    {
        var handlerBind = _receivePipe.Bind<TMessage, TResult, THandler>(routingKey);
    }

    public async Task<TopologyBind> BindReceive<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var handlerBind = _receivePipe.Bind(handler, routingKey);
    }

    public async Task<TopologyBind> BindReceive<TMessage, TResult>(IHandler<TMessage, TResult> handler,
        string routingKey = "")
    {
        var handlerBind = _receivePipe.Bind(handler, routingKey);
    }
}