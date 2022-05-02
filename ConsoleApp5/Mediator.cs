using ConsoleApp5.Pipes;
using ConsoleApp5.Registries;

namespace ConsoleApp5;

internal class Mediator : IMediator
{
    private readonly TopologyRegistry _topologyRegistry;
    private readonly TransportRegistry _transportRegistry;
    private readonly IPipe _pipe;

    public Mediator(TopologyRegistry topologyRegistry, TransportRegistry transportRegistry)
    {
        _topologyRegistry = topologyRegistry;
        _transportRegistry = transportRegistry;
        _pipe = new TopologyPipe(topologyRegistry, transportRegistry);
    }

    public async Task Publish<TMessage>(TMessage message, Action<MessageOptions>? optionsBuilder = null,
        CancellationToken token = default)
    {
        var messageOptions = new MessageOptions();
        optionsBuilder?.Invoke(messageOptions);
        await _pipe.Handle(message, messageOptions, token);
    }

    public async Task<TResult> Send<TMessage, TResult>(TMessage message, Action<MessageOptions>? optionsBuilder = null,
        CancellationToken token = default)
    {
        var messageOptions = new MessageOptions();
        optionsBuilder?.Invoke(messageOptions);
        return await _pipe.Handle<TMessage, TResult>(message, messageOptions, token);
    }

    public void AddTopology<TMessage>(IHandler handler, string pipeName = "default", string routingKey = "")
    {
        _topologyRegistry.AddTopology<TMessage>(handler, pipeName, routingKey);
    }

    public void AddTopology<TMessage, THandler>(string pipeName = "default", string routingKey = "")
        where THandler : IHandler
    {
        _topologyRegistry.AddTopology<TMessage, THandler>(pipeName, routingKey);
    }

    public void RemoveTopology<TMessage>(IHandler handler, string pipeName = "default",
        string routingKey = "")
    {
        _topologyRegistry.RemoveTopology<TMessage>(handler, pipeName, routingKey);
    }

    public void RemoveTopology<TMessage, THandler>(string pipeName = "default", string routingKey = "")
        where THandler : IHandler
    {
        _topologyRegistry.RemoveTopology<TMessage, THandler>(pipeName, routingKey);
    }

    public void AddPipe(string name, IPipe pipe)
    {
        _transportRegistry.AddPipe(name, pipe);
    }

    public void AddPipe<TPipe>(string name) where TPipe : IPipe
    {
        _transportRegistry.AddPipe<TPipe>(name);
    }

    public IEnumerable<(string PipeName, IHandler Handler)> GetTopologies<TMessage>(string routingKey = "")
    {
        return _topologyRegistry.GetTopologies<TMessage>(routingKey);
    }

    public (IPipe, IHandlerRegistry) GetTransport(string transportName)
    {
        return _transportRegistry.GetTransport(transportName);
    }
}