using System.Collections.Concurrent;
using System.Text.Json;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.PublishSubscribe;
using Mediator.Pipes.RequestResponse;
using StackExchange.Redis;

namespace Mediator.Redis.Pipes;

public class RedisMqPipe : IConnectingPipe
{
    private int _isDisposed;
    private readonly ISubscriber _subscriber;
    private readonly IPipeConnector _pipeConnector;
    private readonly Lazy<Task<ChannelMessageQueue>> _responseMq;
    private readonly ConcurrentDictionary<string, Action<ChannelMessage>> _responseHandlers = new();

    public RedisMqPipe(IConnectionMultiplexer connectionMultiplexer, IServiceProvider serviceProvider)
    {
        _subscriber = connectionMultiplexer.GetSubscriber();
        _pipeConnector = new RedisMqPipeConnector(connectionMultiplexer, serviceProvider);
        _responseMq = new Lazy<Task<ChannelMessageQueue>>(CreateResponseMq);
    }

    public async Task PassAsync<TMessage>(MessageContext<TMessage> context,
        CancellationToken token = default) =>
        await _subscriber
            .PublishAsync(context.Route.ToString(), JsonSerializer.Serialize(context))
            .ConfigureAwait(false);

    public async Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> context,
        CancellationToken token = default)
    {
        await _responseMq.Value;

        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        void Handle(ChannelMessage m)
        {
            var responseContext = JsonSerializer.Deserialize<MessageContext<TResult>>(m.Message)!;

            if (responseContext.Message != null) tcs.TrySetResult(responseContext.Message);
            else if (responseContext.ExMessage != null) tcs.TrySetException(new Exception(responseContext.ExMessage));
            else tcs.TrySetException(new InvalidOperationException("Message was not be processed"));
        }

        _responseHandlers.TryAdd(context.CorrelationId, Handle);
        await _subscriber
            .PublishAsync(context.Route.ToString(), JsonSerializer.Serialize(context))
            .ConfigureAwait(false);
        return await tcs.Task;
    }

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default) =>
        await _pipeConnector.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionId, token);

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        await _pipeConnector.ConnectOutAsync<TMessage, TResult>(pipe, routingKey, token);

    private async Task<ChannelMessageQueue> CreateResponseMq()
    {
        void Handle(ChannelMessage m)
        {
            var jsonElement = JsonDocument.Parse(m.Message.ToString()).RootElement;
            var correlationId = jsonElement.GetProperty("CorrelationId").ToString();

            if (_responseHandlers.TryRemove(correlationId, out var handle)) handle(m);
        }

        var channelMq = await _subscriber.SubscribeAsync(RedisWellKnown.ResponseMq).ConfigureAwait(false);
        channelMq.OnMessage(Handle);
        return channelMq;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1) return;

        if (_responseMq.IsValueCreated) await _responseMq.Value.Result.UnsubscribeAsync().ConfigureAwait(false);

        await _pipeConnector.DisposeAsync();
    }
}