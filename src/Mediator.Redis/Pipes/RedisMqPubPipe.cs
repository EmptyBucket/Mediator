using System.Collections.Concurrent;
using System.Text.Json;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.Utils;
using Mediator.Redis.Utils;
using Mediator.Utils;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Mediator.Redis.Pipes;

internal class RedisMqPubPipe : IConnectingPubPipe
{
    private readonly ISubscriber _subscriber;
    private readonly IServiceProvider _serviceProvider;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly ConcurrentDictionary<PipeConnection<IPubPipe>, ChannelMessageQueue> _pipeConnections = new();

    public RedisMqPubPipe(IConnectionMultiplexer multiplexer, IServiceProvider serviceProvider)
    {
        _subscriber = multiplexer.GetSubscriber();
        _serviceProvider = serviceProvider;
        _jsonSerializerOptions = new JsonSerializerOptions { Converters = { ObjectToInferredTypesConverter.Instance } };
    }

    public async Task PassAsync<TMessage>(MessageContext<TMessage> ctx, CancellationToken token = default)
    {
        var redisValue = JsonSerializer.Serialize(ctx, _jsonSerializerOptions);
        var messageProperties = new PropertyBinder<MessageProperties>().Bind(ctx.Meta).Build();
        await _subscriber.PublishAsync(ctx.Route.ToString(), redisValue, messageProperties.Flags).ConfigureAwait(false);
    }

    public IDisposable ConnectOut<TMessage>(IPubPipe pipe, string routingKey = "", string subscriptionId = "")
    {
        var route = Route.For<TMessage>(routingKey);
        var pipeConnection = ConnectPipe<TMessage>(route, pipe);
        return pipeConnection;
    }

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default)
    {
        var route = Route.For<TMessage>(routingKey);
        var pipeConnection = await ConnectPipeAsync<TMessage>(route, pipe);
        return pipeConnection;
    }

    private PipeConnection<IPubPipe> ConnectPipe<TMessage>(Route route, IPubPipe pipe)
    {
        var channelMq = _subscriber.Subscribe(route.ToString());
        channelMq.OnMessage(m => HandleAsync<TMessage>(pipe, m));
        var pipeConnection = new PipeConnection<IPubPipe>(route, pipe, DisconnectPipe, DisconnectPipeAsync);
        _pipeConnections.TryAdd(pipeConnection, channelMq);
        return pipeConnection;
    }

    private async Task<PipeConnection<IPubPipe>> ConnectPipeAsync<TMessage>(Route route, IPubPipe pipe)
    {
        var channelMq = await _subscriber.SubscribeAsync(route.ToString()).ConfigureAwait(false);
        channelMq.OnMessage(m => HandleAsync<TMessage>(pipe, m));
        var pipeConnection = new PipeConnection<IPubPipe>(route, pipe, DisconnectPipe, DisconnectPipeAsync);
        _pipeConnections.TryAdd(pipeConnection, channelMq);
        return pipeConnection;
    }

    private void DisconnectPipe(PipeConnection<IPubPipe> p)
    {
        if (_pipeConnections.TryRemove(p, out var c)) c.Unsubscribe();
    }

    private async ValueTask DisconnectPipeAsync(PipeConnection<IPubPipe> p)
    {
        if (_pipeConnections.TryRemove(p, out var c)) await c.UnsubscribeAsync().ConfigureAwait(false);
    }

    private async Task HandleAsync<TMessage>(IPubPipe pipe, ChannelMessage channelMessage)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var ctx = JsonSerializer.Deserialize<MessageContext<TMessage>>(channelMessage.Message, _jsonSerializerOptions)!;
        ctx = ctx with { DeliveredAt = DateTime.Now, ServiceProvider = scope.ServiceProvider };
        await pipe.PassAsync(ctx);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Keys) await pipeConnection.DisposeAsync();
    }
}