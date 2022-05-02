using System.Data;
using ConsoleApp5.Topologies;

namespace ConsoleApp5.Pipes;

public class TopologyPipe : IPipe
{
    private readonly ITopologyProvider _topologyProvider;

    public TopologyPipe(ITopologyProvider topologyProvider)
    {
        _topologyProvider = topologyProvider;
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options, CancellationToken token)
    {
        var topologies = _topologyProvider.GetTopologies<TMessage>(options.RoutingKey);
        var pipes = topologies.Select(t => _pipeProvider.GetTransport(t.PipeName));
        await Task.WhenAll(pipes.Select(p => p.Handle(message, options, token)));
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token)
    {
        var topologies = _topologyProvider.GetTopologies<TMessage>(options.RoutingKey);
        var pipes = topologies.Select(t => _pipeProvider.GetTransport(t.PipeName)).ToArray();

        if (pipes.Length != 1) throw new InvalidConstraintException("Must be single pipe");

        return await pipes.Single().Handle<TMessage, TResult>(message, options, token);
    }
}