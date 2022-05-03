using System.Data;
using ConsoleApp5.TransportBindings;

namespace ConsoleApp5.Pipes;

public class TransportForkPipe : IPipe
{
    private readonly ITransportBindProvider _transportBindProvider;

    public TransportForkPipe(ITransportBindProvider transportBindProvider)
    {
        _transportBindProvider = transportBindProvider;
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options, CancellationToken token)
    {
        var transportBinds = _transportBindProvider.GetBinds<TMessage>(options.RoutingKey);
        var pipes = transportBinds.Select(t => t.Transport.Pipe);

        await Task.WhenAll(pipes.Select(p => p.Handle(message, options, token)));
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token)
    {
        var transportBinds = _transportBindProvider.GetBinds<TMessage>(options.RoutingKey);
        var pipes = transportBinds.Select(t => t.Transport.Pipe).ToArray();

        if (pipes.Length != 1) throw new InvalidConstraintException("Must be single pipe");

        return await pipes.Single().Handle<TMessage, TResult>(message, options, token);
    }
}