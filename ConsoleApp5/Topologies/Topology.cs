using ConsoleApp5.Bindings;
using ConsoleApp5.Pipes;

namespace ConsoleApp5.Topologies;

public class Topology : ITopology
{
    private readonly IPipe _dispatchPipe;
    private readonly IPipe _receivePipe;
    private readonly IPipeBinder _dispatchPipeBinder;
    private readonly IPipeBinder _receivePipeBinder;
    private readonly IHandlerBinder _handlerBinder;

    public Topology(IPipe dispatchPipe, IPipe receivePipe,
        IPipeBinder dispatchPipeBinder, IPipeBinder receivePipeBinder,
        IHandlerBinder handlerBinder)
    {
        _dispatchPipe = dispatchPipe;
        _receivePipe = receivePipe;
        _dispatchPipeBinder = dispatchPipeBinder;
        _receivePipeBinder = receivePipeBinder;
        _handlerBinder = handlerBinder;
    }

    public async Task BindDispatch<TMessage>(string routingKey = "")
    {
        await _dispatchPipeBinder.Bind<TMessage>(_dispatchPipe, routingKey);
    }
    
    public async Task BindDispatch<TMessage, TResult>(string routingKey = "")
    {
        await _dispatchPipeBinder.Bind<TMessage, TResult>(_dispatchPipe, routingKey);
    }

    public async Task BindReceive<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        await _receivePipeBinder.Bind<TMessage>(_receivePipe);
        _handlerBinder.Bind(handler, routingKey);
    }

    public async Task BindReceive<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        await _receivePipeBinder.Bind<TMessage>(_receivePipe);
        _handlerBinder.Bind<TMessage, THandler>(routingKey);
    }

    public async Task BindReceive<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "")
    {
        await _receivePipeBinder.Bind<TMessage, TResult>(_receivePipe);
        _handlerBinder.Bind(handler, routingKey);
    }

    public async Task BindReceive<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>
    {
        await _receivePipeBinder.Bind<TMessage, TResult>(_receivePipe);
        _handlerBinder.Bind<TMessage, THandler>(routingKey);
    }

    public async Task UnbindDispatch<TMessage>(string routingKey = "")
    {
        await _dispatchPipeBinder.Unbind<TMessage>(_dispatchPipe, routingKey);
    }

    public async Task UnbindReceive<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        await _receivePipeBinder.Unbind<TMessage>(_receivePipe);
        _handlerBinder.Unbind(handler, routingKey);
    }

    public async Task UnbindReceive<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        await _receivePipeBinder.Unbind<TMessage>(_receivePipe);
        _handlerBinder.Unbind<TMessage, THandler>(routingKey);
    }
}