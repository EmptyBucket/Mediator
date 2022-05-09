using System.Collections.Concurrent;
using System.Text.Json;
using FlexMediator.Utils;
using StackExchange.Redis;

namespace FlexMediator.Pipes;

public class RedisMqPipe : IPipe, IPipeConnector
{
    private int _isDisposed;
    private readonly ISubscriber _subscriber;
    private readonly IPipeConnector _pipeConnector;
    private readonly Lazy<Task<ChannelMessageQueue>> _responsesMq;
    private readonly ConcurrentDictionary<string, Action<string>> _responseActions = new();

    public RedisMqPipe(ISubscriber subscriber, IServiceProvider serviceProvider)
    {
        _subscriber = subscriber;
        _pipeConnector = new RedisMqPipeConnector(subscriber, serviceProvider);
        _responsesMq = new Lazy<Task<ChannelMessageQueue>>(async () =>
        {
            var channelMq = await _subscriber.SubscribeAsync("responses").ConfigureAwait(false);
            channelMq.OnMessage(r =>
            {
                var correlationId =
                    JsonDocument.Parse(r.ToString()).RootElement.GetProperty("CorrelationId").ToString();
                _responseActions[correlationId](r.Message);
            });
            return channelMq;
        });
    }

    public async Task PassAsync<TMessage>(TMessage message, MessageContext context,
        CancellationToken token = default)
    {
        var route = Route.For<TMessage>(context.RoutingKey);
        await _subscriber.PublishAsync(route.ToString(), new RedisValue(JsonSerializer.Serialize(message)))
            .ConfigureAwait(false);
    }

    public async Task<TResult> PassAsync<TMessage, TResult>(TMessage message, MessageContext context,
        CancellationToken token = default)
    {
        await _responsesMq.Value;

        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _responseActions.TryAdd(correlationId, r =>
        {
            var response = JsonSerializer.Deserialize<RedisMqMessage<TResult>>(r);

            if (response.Value is not null) tcs.SetResult(response.Value);
            else if (response.Exception is not null) tcs.SetException(new Exception(response.Exception));
            else tcs.SetException(new ArgumentException("Message was not be processed"));
        });

        try
        {
            var route = Route.For<TMessage, TResult>(context.RoutingKey);
            var request = new RedisMqMessage<TMessage>(correlationId, message);
            await _subscriber.PublishAsync(route.ToString(), new RedisValue(JsonSerializer.Serialize(request)))
                .ConfigureAwait(false);
            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            _responseActions.TryRemove(correlationId, out _);
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
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1) return;

        if (_responsesMq.IsValueCreated) await _responsesMq.Value.Result.UnsubscribeAsync().ConfigureAwait(false);

        await _pipeConnector.DisposeAsync();
    }
}