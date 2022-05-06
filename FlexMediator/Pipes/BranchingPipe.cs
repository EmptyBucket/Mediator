using System.Data;

namespace FlexMediator.Pipes;

public class BranchingPipe : IPipe, IPipeBinder
{
    private readonly PipeBinder _pipeBinder;

    public BranchingPipe()
    {
        _pipeBinder = new PipeBinder();
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options, CancellationToken token)
    {
        var route = new Route(typeof(TMessage), options.RoutingKey);
        var pipeBindings = _pipeBinder.GetValueOrDefault(route) ?? Enumerable.Empty<PipeBind>();
        var pipes = pipeBindings.Select(t => t.Pipe);

        await Task.WhenAll(pipes.Select(p => p.Handle(message, options, token)));
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token)
    {
        var route = new Route(typeof(TMessage), options.RoutingKey, typeof(TResult));
        var pipeBindings = _pipeBinder.GetValueOrDefault(route) ?? Enumerable.Empty<PipeBind>();
        var pipes = pipeBindings.Select(t => t.Pipe).ToArray();

        if (pipes.Length != 1) throw new InvalidConstraintException("Must be single pipe");

        return await pipes.Single().Handle<TMessage, TResult>(message, options, token);
    }

    public Task<PipeBind> Bind<TMessage>(IPipe pipe, string routingKey = "")
    {
        return _pipeBinder.Bind<TMessage>(pipe, routingKey);
    }

    public Task<PipeBind> Bind<TMessage, TResult>(IPipe pipe, string routingKey = "")
    {
        return _pipeBinder.Bind<TMessage, TResult>(pipe, routingKey);
    }
}