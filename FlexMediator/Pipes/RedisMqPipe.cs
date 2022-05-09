using System.Collections.Concurrent;
using System.Text.Json;
using FlexMediator.Utils;
using StackExchange.Redis;

namespace FlexMediator.Pipes;

public class RedisMqPipe : IPipe, IPipeConnector
{
    private readonly ISubscriber _subscriber;
    private readonly IPipeConnector _pipeConnector;
    private ulong _responsesMqIsSubscribed;
    private ChannelMessageQueue? _responsesMq;
    private readonly ConcurrentDictionary<string, Action<string>> _responseActions = new();

    public RedisMqPipe(ISubscriber subscriber, IServiceProvider serviceProvider)
    {
        _subscriber = subscriber;
        _pipeConnector = new RedisMqPipeConnector(subscriber, serviceProvider);
    }

    public async Task PassAsync<TMessage>(TMessage message, MessageOptions options,
        CancellationToken token = default)
    {
        var route = Route.For<TMessage>(options.RoutingKey);
        await _subscriber.PublishAsync(route.ToString(), new RedisValue(JsonSerializer.Serialize(message)));
    }

    public async Task<TResult> PassAsync<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token = default)
    {
        await EnsureResponsesMqIsSubscribed();

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
            var route = Route.For<TMessage, TResult>(options.RoutingKey);
            var request = new RedisMessage<TMessage>(correlationId, message);
            await _subscriber.PublishAsync(route.ToString(), new RedisValue(JsonSerializer.Serialize(request)));
            return await tcs.Task;
        }
        finally
        {
            _responseActions.TryRemove(correlationId, out _);
        }
    }

    private async Task EnsureResponsesMqIsSubscribed()
    {
        if (Interlocked.CompareExchange(ref _responsesMqIsSubscribed, 1, 0) == 0)
        {
            _responsesMq = await _subscriber.SubscribeAsync("responses");
            _responsesMq.OnMessage(r =>
            {
                var correlationId =
                    JsonDocument.Parse(r.ToString()).RootElement.GetProperty("CorrelationId").ToString();
                _responseActions[correlationId](r.Message);
            });
        }
    }

    public async Task<PipeConnection> ConnectInAsync<TMessage>(IPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        await _pipeConnector.ConnectInAsync<TMessage>(pipe, routingKey, token);

    public async Task<PipeConnection> ConnectInAsync<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        await _pipeConnector.ConnectInAsync<TMessage, TResult>(pipe, routingKey, token);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _responsesMqIsSubscribed, 0, 1) == 1)
            await _responsesMq!.UnsubscribeAsync();

        await _pipeConnector.DisposeAsync();
    }
}

internal readonly record struct RedisMessage<T>(string CorrelationId, T? Value = default, string? Exception = null);