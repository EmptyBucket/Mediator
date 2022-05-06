using System.Data;

namespace FlexMediator.Pipes;

public class BranchingPipe : IPipe
{
    private readonly IPipeBindings _pipeBindings;

    public BranchingPipe(IPipeBindings pipeBindings)
    {
        _pipeBindings = pipeBindings;
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options, CancellationToken token)
    {
        var route = new Route(typeof(TMessage), options.RoutingKey);
        var pipeBindings = _pipeBindings.GetValueOrDefault(route) ?? Enumerable.Empty<PipeBind>();
        var pipes = pipeBindings.Select(t => t.Pipe);

        await Task.WhenAll(pipes.Select(p => p.Handle(message, options, token)));
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token)
    {
        var route = new Route(typeof(TMessage), options.RoutingKey, typeof(TResult));
        var pipeBindings = _pipeBindings.GetValueOrDefault(route) ?? Enumerable.Empty<PipeBind>();
        var pipes = pipeBindings.Select(t => t.Pipe).ToArray();

        if (pipes.Length != 1) throw new InvalidConstraintException("Must be single pipe");

        return await pipes.Single().Handle<TMessage, TResult>(message, options, token);
    }
}