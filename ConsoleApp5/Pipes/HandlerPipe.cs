using System.Data;
using ConsoleApp5.Registries;

namespace ConsoleApp5.Pipes;

public class HandlerPipe : IPipe
{
    private readonly ITopologyProvider _topologyProvider;

    public HandlerPipe(ITopologyProvider topologyProvider)
    {
        _topologyProvider = topologyProvider;
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options, CancellationToken token)
    {
        var topologies = _topologyProvider.GetTopologies<TMessage>(options.RoutingKey);
        var handlers = topologies.Select(t => t.Handler);
        await Task.WhenAll(handlers.Cast<IHandler<TMessage>>().Select(h => h.Handle(message, options, token)));
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token)
    {
        var topologies = _topologyProvider.GetTopologies<TMessage>(options.RoutingKey);
        var handlers = topologies.Select(t => t.Handler).ToArray();

        if (handlers.Length != 1) throw new InvalidConstraintException("Must be single handler");

        return await handlers.Cast<IHandler<TMessage, TResult>>().Single().Handle(message, options, token);
    }
}