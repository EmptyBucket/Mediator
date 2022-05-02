using System.Data;
using ConsoleApp5.Topologies;

namespace ConsoleApp5.Pipes;

public class TopologyPipe : IPipe
{
    private readonly IEnumerable<ITopologyProvider> _topologyProviders;

    public TopologyPipe(IEnumerable<ITopologyProvider> topologyProviders)
    {
        _topologyProviders = topologyProviders;
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options, CancellationToken token)
    {
        var topologies =
            _topologyProviders.Select(p => p.GetTopology<TMessage>(options.RoutingKey)).Where(t => t.HasValue);
        var pipes = topologies.Select(t => t!.Value.Pipe);

        await Task.WhenAll(pipes.Select(p => p.Handle(message, options, token)));
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token)
    {
        var topologies =
            _topologyProviders.Select(p => p.GetTopology<TMessage>(options.RoutingKey)).Where(t => t.HasValue);
        var pipes = topologies.Select(t => t!.Value.Pipe).ToArray();

        if (pipes.Length != 1) throw new InvalidConstraintException("Must be single pipe");

        return await pipes.Single().Handle<TMessage, TResult>(message, options, token);
    }
}