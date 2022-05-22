using System.Collections.Concurrent;
using System.Text.Json;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.Utils;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Mediator.Redis.Pipes;

internal class RedisMqPubPipe : IConnectingPubPipe
{
    private readonly ISubscriber _subscriber;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<PipeConnection<IPubPipe>, ChannelMessageQueue> _pipeConnections = new();

    public RedisMqPubPipe(IConnectionMultiplexer multiplexer, IServiceProvider serviceProvider)
    {
        _subscriber = multiplexer.GetSubscriber();
        _serviceProvider = serviceProvider;
    }

    public async Task PassAsync<TMessage>(MessageContext<TMessage> ctx, CancellationToken token = default)
    {
        var redisValue = JsonSerializer.Serialize(ctx);
        await _subscriber.PublishAsync(ctx.Route.ToString(), redisValue).ConfigureAwait(false);
    }

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default)
    {
        var route = Route.For<TMessage>(routingKey);
        var pipeConnection = await ConnectPipe<TMessage>(route, pipe);
        return pipeConnection;
    }

    private async Task<PipeConnection<IPubPipe>> ConnectPipe<TMessage>(Route route, IPubPipe pipe)
    {
        async Task HandleAsync(ChannelMessage m)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var ctx = JsonSerializer.Deserialize<MessageContext<TMessage>>(m.Message)!;
            ctx = ctx with { DeliveredAt = DateTimeOffset.Now, ServiceProvider = scope.ServiceProvider };
            await pipe.PassAsync(ctx);
        }

        var channelMq = await _subscriber.SubscribeAsync(route.ToString()).ConfigureAwait(false);
        channelMq.OnMessage(HandleAsync);
        var pipeConnection = new PipeConnection<IPubPipe>(route, pipe, DisconnectPipe);
        _pipeConnections.TryAdd(pipeConnection, channelMq);
        return pipeConnection;
    }

    private async ValueTask DisconnectPipe(PipeConnection<IPubPipe> p)
    {
        if (_pipeConnections.TryRemove(p, out var c)) await c.UnsubscribeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Keys) await pipeConnection.DisposeAsync();
    }
}