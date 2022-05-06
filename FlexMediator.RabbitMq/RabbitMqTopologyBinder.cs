using EasyNetQ;
using FlexMediator.Handlers;
using FlexMediator.Pipes;
using FlexMediator.Topologies;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediatorRabbit;

public class RabbitMqTopologyBinder : ITopologyBinder
{
    private readonly RabbitMqPipe _boundDispatchPipe;
    private readonly HandlingPipe _boundReceivePipe;
    private readonly BranchingPipe _dispatchPipe;
    private readonly RabbitMqPipeBinder _receivePipeBinder;

    public RabbitMqTopologyBinder(BranchingPipe dispatchPipe, IBus bus, IServiceScopeFactory serviceScopeFactory)
    {
        _dispatchPipe = dispatchPipe;
        _receivePipeBinder = new RabbitMqPipeBinder(bus);
        
        _boundDispatchPipe = new RabbitMqPipe(bus);
        _boundReceivePipe = new HandlingPipe(serviceScopeFactory);
    }

    public async Task<TopologyBind> BindDispatch<TMessage>(string routingKey = "")
    {
        var pipeBind = await _dispatchPipe.Bind<TMessage>(_boundDispatchPipe, routingKey);
    }

    public async Task<TopologyBind> BindDispatch<TMessage, TResult>(string routingKey = "")
    {
        var pipeBind = await _dispatchPipe.Bind<TMessage, TResult>(_boundDispatchPipe, routingKey);
    }

    public async Task<TopologyBind> BindReceive<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var pipeBind = await _receivePipeBinder.Bind<TMessage>(_boundReceivePipe);
        var handlerBind = _boundReceivePipe.Bind<TMessage, THandler>(routingKey);
    }

    public async Task<TopologyBind> BindReceive<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>
    {
        var pipeBind = await _receivePipeBinder.Bind<TMessage, TResult>(_boundReceivePipe);
        var handlerBind = _boundReceivePipe.Bind<TMessage, TResult, THandler>(routingKey);
    }

    public async Task<TopologyBind> BindReceive<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var pipeBind = await _receivePipeBinder.Bind<TMessage>(_boundReceivePipe);
        var handlerBind = _boundReceivePipe.Bind(handler, routingKey);
    }

    public async Task<TopologyBind> BindReceive<TMessage, TResult>(IHandler<TMessage, TResult> handler,
        string routingKey = "")
    {
        var pipeBind = await _receivePipeBinder.Bind<TMessage, TResult>(_boundReceivePipe);
        var handlerBind = _boundReceivePipe.Bind(handler, routingKey);
    }
}