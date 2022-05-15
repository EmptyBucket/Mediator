using System.Collections.Concurrent;
using System.Text.Json;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.PublishSubscribe;
using Mediator.Pipes.RequestResponse;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Mediator.Redis.Pipes;

public class RedisMqPipeConnector : IPipeConnector
{
    private readonly ISubscriber _subscriber;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<PipeConnection, PipeConnection> _pipeConnections = new();

    public RedisMqPipeConnector(IConnectionMultiplexer connectionMultiplexer, IServiceProvider serviceProvider)
    {
        _subscriber = connectionMultiplexer.GetSubscriber();
        _serviceProvider = serviceProvider;
    }

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default)
    {
        async Task Handle(ChannelMessage m)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var context = JsonSerializer.Deserialize<MessageContext<TMessage>>(m.Message)!;
            context = context with { DeliveredAt = DateTimeOffset.Now, ServiceProvider = scope.ServiceProvider };
            await pipe.PassAsync(context, token);
        }

        var route = Route.For<TMessage>(routingKey);
        var channelMq = await _subscriber.SubscribeAsync(route.ToString()).ConfigureAwait(false);
        channelMq.OnMessage(Handle);
        var pipeConnection = new PipeConnection(channelMq, p => _pipeConnections.TryRemove(p, out _));
        _pipeConnections.TryAdd(pipeConnection, pipeConnection);
        return pipeConnection;
    }

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        async Task Handle(ChannelMessage m)
        {
            var requestContext = JsonSerializer.Deserialize<MessageContext<TMessage>>(m.Message)!;

            try
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                requestContext =
                    requestContext with { DeliveredAt = DateTimeOffset.Now, ServiceProvider = scope.ServiceProvider };
                var result = await pipe.PassAsync<TMessage, TResult>(requestContext, token);
                var responseContext = new MessageContext<TResult>(requestContext.Route, Guid.NewGuid().ToString(),
                    requestContext.CorrelationId, DateTimeOffset.Now) { Message = result };
                await _subscriber
                    .PublishAsync(RedisWellKnown.ResponseMq, JsonSerializer.Serialize(responseContext))
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                var responseContext = new MessageContext<TResult>(requestContext.Route, Guid.NewGuid().ToString(),
                    requestContext.CorrelationId, DateTimeOffset.Now) { ExMessage = e.Message };
                await _subscriber
                    .PublishAsync(RedisWellKnown.ResponseMq, JsonSerializer.Serialize(responseContext))
                    .ConfigureAwait(false);
                throw;
            }
        }

        var route = Route.For<TMessage, TResult>(routingKey);
        var channelMq = await _subscriber.SubscribeAsync(route.ToString()).ConfigureAwait(false);
        channelMq.OnMessage(Handle);
        var pipeConnection = new PipeConnection(channelMq, p => _pipeConnections.TryRemove(p, out _));
        _pipeConnections.TryAdd(pipeConnection, pipeConnection);
        return pipeConnection;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Values) await pipeConnection.DisposeAsync();
    }

    private class PipeConnection : IAsyncDisposable
    {
        private readonly ChannelMessageQueue _channelMessageQueue;
        private readonly Action<PipeConnection> _callback;

        public PipeConnection(ChannelMessageQueue channelMessageQueue, Action<PipeConnection> callback)
        {
            _channelMessageQueue = channelMessageQueue;
            _callback = callback;
        }

        public async ValueTask DisposeAsync()
        {
            await _channelMessageQueue.UnsubscribeAsync();
            _callback(this);
        }
    }
}