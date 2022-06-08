using System.Collections.Immutable;
using Mediator.Handlers;
using Mediator.Pipes.Utils;

namespace Mediator;

internal class Mediator : IMediator
{
    public Mediator(MediatorTopology topology)
    {
        Topology = topology;
    }

    public async Task PublishAsync<TMessage>(TMessage message, Options? options = null,
        CancellationToken token = default)
    {
        options ??= new Options();
        var route = Route.For<TMessage>(options.RoutingKey);
        var ctx = CreateMessageContext(route, message, options);
        await Topology.Dispatch.PassAsync(ctx, token);
    }

    public async Task<TResult> SendAsync<TMessage, TResult>(TMessage message, Options? options = null,
        CancellationToken token = default)
    {
        options ??= new Options();
        var route = Route.For<TMessage, TResult>(options.RoutingKey);
        var ctx = CreateMessageContext(route, message, options);
        return await Topology.Dispatch.PassAsync<TMessage, TResult>(ctx, token);
    }

    public MediatorTopology Topology { get; }

    public ValueTask DisposeAsync() => Topology.DisposeAsync();

    private static MessageContext<TMessage> CreateMessageContext<TMessage>(Route route, TMessage message,
        Options options)
    {
        var meta = options.Meta.ToImmutableDictionary();
        var extra = options.Extra.ToImmutableDictionary();
        var ctx = new MessageContext<TMessage>(route, message, meta, extra) { CreatedAt = DateTimeOffset.Now };
        return ctx;
    }
}