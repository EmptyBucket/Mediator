using System.Collections.Concurrent;
using System.Text.Json;
using FlexMediator.Utils;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace FlexMediator.Pipes;

public class RedisMqPipeConnector : IPipeConnector
{
    private readonly ISubscriber _subscriber;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<PipeConnection, PipeConnection> _pipeConnections = new();

    public RedisMqPipeConnector(ISubscriber subscriber, IServiceProvider serviceProvider)
    {
        _subscriber = subscriber;
        _serviceProvider = serviceProvider;
    }

    public async Task<PipeConnection> ConnectInAsync<TMessage>(IPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = Route.For<TMessage>(routingKey);
        var channelMq = await _subscriber.SubscribeAsync(route.ToString());
        channelMq.OnMessage(async r =>
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var message = JsonSerializer.Deserialize<TMessage>(r.Message)!;
            await pipe.PassAsync<TMessage>(message, new MessageOptions(scope.ServiceProvider, routingKey), token);
        });
        var pipeConnection = new PipeConnection(route, pipe, p => Disconnect(p, () => channelMq.UnsubscribeAsync()));
        Connect(pipeConnection);
        return pipeConnection;
    }

    public async Task<PipeConnection> ConnectInAsync<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>(routingKey);
        var channelMq = await _subscriber.SubscribeAsync(route.ToString());
        channelMq.OnMessage(async r =>
        {
            var request = JsonSerializer.Deserialize<RedisMessage<TMessage>>(r.Message);

            try
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var result = await pipe.PassAsync<TMessage, TResult>(request.Value!,
                    new MessageOptions(scope.ServiceProvider, routingKey), token);
                var response = new RedisMessage<TResult>(request.CorrelationId, result);
                await _subscriber.PublishAsync("responses", new RedisValue(JsonSerializer.Serialize(response)));
            }
            catch (Exception e)
            {
                var response = new RedisMessage<TResult>(request.CorrelationId, Exception: e.Message);
                await _subscriber.PublishAsync("responses", new RedisValue(JsonSerializer.Serialize(response)));
                throw;
            }
        });
        var pipeConnection = new PipeConnection(route, pipe, p => Disconnect(p, () => channelMq.UnsubscribeAsync()));
        Connect(pipeConnection);
        return pipeConnection;
    }

    private void Connect(PipeConnection pipeConnection) => _pipeConnections.TryAdd(pipeConnection, pipeConnection);

    private ValueTask Disconnect(PipeConnection pipeConnection, Func<Task> unsubscribe)
    {
        if (_pipeConnections.TryRemove(pipeConnection, out _)) unsubscribe();

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Values) await pipeConnection.DisposeAsync();
    }
}