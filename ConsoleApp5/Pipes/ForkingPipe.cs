using System.Data;
using ConsoleApp5.Registries;

namespace ConsoleApp5.Pipes;

public class ForkingPipe : IPipe
{
    private readonly IPipeProvider _pipeProvider;

    public ForkingPipe(IPipeProvider pipeProvider)
    {
        _pipeProvider = pipeProvider;
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options, CancellationToken token)
    {
        var pipes = _pipeProvider.GetPipes<TMessage>();
        await Task.WhenAll(pipes.Select(p => p.Handle(message, options, token)));
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token)
    {
        var pipes = _pipeProvider.GetPipes<TMessage>();

        if (pipes.Count != 1) throw new InvalidConstraintException("Must be single pipe");

        return await pipes.Single().Handle<TMessage, TResult>(message, options, token);
    }
}