using ConsoleApp5.Pipes;
using ConsoleApp5.Registries;

namespace ConsoleApp5;

internal class Mediator : IMediator, ITopologyRegistry
{
    private readonly IPipe _pipe;
    private readonly ITopologyRegistry _topologyRegistry;

    public Mediator(IPipe pipe, ITopologyRegistry topologyRegistry)
    {
        _pipe = pipe;
        _topologyRegistry = topologyRegistry;
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

    public void AddTopology<TMessage>(IHandler handler, string transportName = "default", string routingKey = "")
    {
        _topologyRegistry.AddTopology<TMessage>(handler, transportName, routingKey);
    }

    public void AddTopology<TMessage, THandler>(string transportName = "default", string routingKey = "")
        where THandler : IHandler
    {
        _topologyRegistry.AddTopology<TMessage, THandler>(transportName, routingKey);
    }

    public void RemoveTopology<TMessage>(IHandler handler, string routingKey = "")
    {
        _topologyRegistry.RemoveTopology<TMessage>(handler, routingKey);
    }

    public void RemoveTopology<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler
    {
        _topologyRegistry.RemoveTopology<TMessage, THandler>(routingKey);
    }
}