using System.Data;

namespace FlexMediator.Pipes;

public class ForkPipe : IPipe
{
    private readonly IPipeBindProvider _pipeBindProvider;

    public ForkPipe(IPipeBindProvider pipeBindProvider)
    {
        _pipeBindProvider = pipeBindProvider;
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options, CancellationToken token)
    {
        var pipeBindings = _pipeBindProvider.GetBindings<TMessage>(options.RoutingKey);
        var pipes = pipeBindings.Select(t => t.Pipe);

        await Task.WhenAll(pipes.Select(p => p.Handle(message, options, token)));
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token)
    {
        var pipeBindings = _pipeBindProvider.GetBindings<TMessage, TResult>(options.RoutingKey);
        var pipes = pipeBindings.Select(t => t.Pipe).ToArray();

        if (pipes.Length != 1) throw new InvalidConstraintException("Must be single pipe");

        return await pipes.Single().Handle<TMessage, TResult>(message, options, token);
    }
}