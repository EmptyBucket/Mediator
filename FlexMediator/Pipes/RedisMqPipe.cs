using System.Collections.Concurrent;
using System.Text.Json;
using FlexMediator.Utils;
using StackExchange.Redis;

namespace FlexMediator.Pipes;

public class RedisMqPipe : IPipe, IPipeConnector
{
    private readonly ISubscriber _subscriber;
    private readonly IPipeConnector _pipeConnector;
    private readonly ConcurrentDictionary<string, Action<string>> _responseActions = new();

    public RedisMqPipe(ISubscriber subscriber)
    {
        _subscriber = subscriber;
        _pipeConnector = new RedisMqPipeConnector(subscriber);
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options,
        CancellationToken token = default)
    {
        var route = new Route(typeof(TMessage), options.RoutingKey);
        await _subscriber.PublishAsync(route.ToString(), new RedisValue(JsonSerializer.Serialize(message)));
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token = default)
    {
        await EnsureResponsesMqExist();

        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _responseActions.TryAdd(correlationId, r =>
        {
            var response = JsonSerializer.Deserialize<RedisMessage<TResult>>(r);

            if (response.Value is not null) tcs.SetResult(response.Value);
            else if (response.Exception is not null) tcs.SetException(new Exception(response.Exception));
            else tcs.SetException(new ArgumentException("Message was not be processed"));
        });

        try
        {
            var route = new Route(typeof(TMessage), options.RoutingKey, typeof(TResult));
            var request = new RedisMessage<TMessage>(correlationId, message);
            await _subscriber.PublishAsync(route.ToString(), new RedisValue(JsonSerializer.Serialize(request)));
            return await tcs.Task;
        }
        finally
        {
            _responseActions.TryRemove(correlationId, out _);
        }
    }

    private async Task EnsureResponsesMqExist()
    {
        if (!_subscriber.IsConnected("responses"))
            await _subscriber.SubscribeAsync("responses", (_, r) =>
            {
                var correlationId =
                    JsonDocument.Parse(r.ToString()).RootElement.GetProperty("CorrelationId").ToString();
                _responseActions[correlationId].Invoke(r);
            });
    }

    public async Task<PipeConnection> Out<TMessage>(IPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        await _pipeConnector.Out<TMessage>(pipe, routingKey, token);

    public async Task<PipeConnection> Out<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        await _pipeConnector.Out<TMessage, TResult>(pipe, routingKey, token);
}

internal readonly record struct RedisMessage<T>(string CorrelationId, T? Value = default, string? Exception = null);