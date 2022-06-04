using Mediator.Handlers;
using Mediator.Pipes.Utils;

namespace Mediator;

internal class Mediator : IMediator
{
    public Mediator(MediatorTopology topology)
    {
        Topology = topology;
    }

    public async Task PublishAsync<TMessage>(TMessage message,
        Action<MessageContext<TMessage>>? ctxBuilder = null, CancellationToken token = default)
    {
        var route = Route.For<TMessage>();
        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var ctx = new MessageContext<TMessage>(route, messageId, correlationId, DateTimeOffset.Now, message);
        ctxBuilder?.Invoke(ctx);
        await Topology.Dispatch.PassAsync(ctx, token);
    }

    public async Task<TResult> SendAsync<TMessage, TResult>(TMessage message,
        Action<MessageContext<TMessage>>? ctxBuilder = null, CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>();
        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var ctx = new MessageContext<TMessage>(route, messageId, correlationId, DateTimeOffset.Now, message);
        ctxBuilder?.Invoke(ctx);
        return await Topology.Dispatch.PassAsync<TMessage, TResult>(ctx, token);
    }

    public MediatorTopology Topology { get; }

    public ValueTask DisposeAsync() => Topology.DisposeAsync();
}