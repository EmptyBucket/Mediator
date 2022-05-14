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
        var route = Route.For<TMessage>(routingKey);
        var channelMq = await _subscriber.SubscribeAsync(route.ToString()).ConfigureAwait(false);
        channelMq.OnMessage(async r =>
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var message = JsonSerializer.Deserialize<TMessage>(r.Message)!;
            var messageContext = new MessageContext(scope.ServiceProvider, routingKey);
            await pipe.PassAsync<TMessage>(message, messageContext, token);
        });
        var pipeConnection = new PipeConnection(channelMq, Disconnect);
        Connect(pipeConnection);
        return pipeConnection;
    }

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>(routingKey);
        var channelMq = await _subscriber.SubscribeAsync(route.ToString()).ConfigureAwait(false);
        channelMq.OnMessage(async r =>
        {
            var request = JsonSerializer.Deserialize<RedisMqMessage<TMessage>>(r.Message);

            try
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var messageContext = new MessageContext(scope.ServiceProvider, routingKey);
                var result = await pipe.PassAsync<TMessage, TResult>(request.Value!, messageContext, token);
                var response = new RedisMqMessage<TResult>(request.CorrelationId, result);
                await _subscriber.PublishAsync(RedisMqWellKnown.ResponsesMq, JsonSerializer.Serialize(response))
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                var response = new RedisMqMessage<TResult>(request.CorrelationId, Exception: e.Message);
                await _subscriber.PublishAsync(RedisMqWellKnown.ResponsesMq, JsonSerializer.Serialize(response))
                    .ConfigureAwait(false);
                throw;
            }
        });
        var pipeConnection = new PipeConnection(channelMq, Disconnect);
        Connect(pipeConnection);
        return pipeConnection;
    }

    private void Connect(PipeConnection pipeConnection) => _pipeConnections.TryAdd(pipeConnection, pipeConnection);

    private void Disconnect(PipeConnection pipeConnection) => _pipeConnections.TryRemove(pipeConnection, out _);

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