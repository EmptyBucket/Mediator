using FlexMediator.Handlers;

namespace FlexMediator.Topologies;

public interface ITopologyBinder
{
    Task<TopologyBind> BindDispatch<TMessage>(string routingKey = "");
    
    Task<TopologyBind> BindDispatch<TMessage, TResult>(string routingKey = "");

    Task<TopologyBind> BindReceive<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>;

    Task<TopologyBind> BindReceive<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>;

    Task<TopologyBind> BindReceive<TMessage>(IHandler<TMessage> handler, string routingKey = "");

    Task<TopologyBind> BindReceive<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "");
}