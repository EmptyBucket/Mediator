using FlexMediator.Utils;

namespace FlexMediator.Pipes;

public class Pipe : IPipe, IPipeConnector
{
    private readonly PipeConnector _pipeConnector;

    public Pipe()
    {
        _pipeConnector = new PipeConnector();
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options,
        CancellationToken token = default)
    {
        var route = new Route(typeof(TMessage), options.RoutingKey);
        var connections = _pipeConnector.GetValueOrDefault(route) ?? Enumerable.Empty<PipeConnection>();
        var pipes = connections.Select(c => c.Pipe);
        await Task.WhenAll(pipes.Select(p => p.Handle(message, options, token)));
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token = default)
    {
        var route = new Route(typeof(TMessage), options.RoutingKey, typeof(TResult));
        var connections = _pipeConnector.GetValueOrDefault(route) ?? Enumerable.Empty<PipeConnection>();
        var pipes = connections.Select(c => c.Pipe);
        return await pipes.Single().Handle<TMessage, TResult>(message, options, token);
    }

    public Task<PipeConnection> Out<TMessage>(IPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        _pipeConnector.Out<TMessage>(pipe, routingKey, token);

    public Task<PipeConnection> Out<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        _pipeConnector.Out<TMessage, TResult>(pipe, routingKey, token);
}