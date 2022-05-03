using System.Data;

namespace FlexMediator.Pipes;

public class ForkPipe : IPipe
{
    private readonly IPipeBindings _pipeBindings;

    public ForkPipe(IPipeBindings pipeBindings)
    {
        _pipeBindings = pipeBindings;
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options, CancellationToken token)
    {
        var route = new Route(typeof(TMessage), RoutingKey: options.RoutingKey);
        var pipeBindings = _pipeBindings.GetValueOrDefault(route) ?? Enumerable.Empty<PipeBind>();
        var pipes = pipeBindings.Select(t => t.Pipe);

        await Task.WhenAll(pipes.Select(p => p.Handle(message, options, token)));
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token)
    {
        var route = new Route(typeof(TMessage), ResultType: typeof(TResult), RoutingKey: options.RoutingKey);
        var pipeBindings = _pipeBindings.GetValueOrDefault(route) ?? Enumerable.Empty<PipeBind>();
        var pipes = pipeBindings.Select(t => t.Pipe).ToArray();

        if (pipes.Length != 1) throw new InvalidConstraintException("Must be single pipe");

        return await pipes.Single().Handle<TMessage, TResult>(message, options, token);
    }
}