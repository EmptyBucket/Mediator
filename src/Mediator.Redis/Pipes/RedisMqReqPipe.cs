using System.Collections.Concurrent;
using System.Text.Json;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.Utils;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Mediator.Redis.Pipes;

internal class RedisMqReqPipe : IConnectingReqPipe
{
    private readonly ISubscriber _subscriber;
    private readonly IServiceProvider _serviceProvider;
    private readonly Lazy<Task<ChannelMessageQueue>> _responseMq;
    private readonly ConcurrentDictionary<string, Action<ChannelMessage>> _responseHandlers = new();
    private readonly ConcurrentDictionary<PipeConnection<IReqPipe>, PipeConnection<IReqPipe>> _pipeConnections = new();

    public RedisMqReqPipe(IConnectionMultiplexer multiplexer, IServiceProvider serviceProvider)
    {
        _subscriber = multiplexer.GetSubscriber();
        _serviceProvider = serviceProvider;
        _responseMq = new Lazy<Task<ChannelMessageQueue>>(CreateResponseMq);
    }

    public async Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> ctx,
        CancellationToken token = default)
    {
        await _responseMq.Value;

        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        void Handle(ChannelMessage m)
        {
            var resultCtx = JsonSerializer.Deserialize<ResultContext<TResult>>(m.Message)!;

            if (resultCtx.Result != null) tcs.TrySetResult(resultCtx.Result);
            else if (resultCtx.Exception != null) tcs.TrySetException(resultCtx.Exception);
            else tcs.TrySetException(new InvalidOperationException("Message was not be processed"));
        }

        _responseHandlers.TryAdd(ctx.CorrelationId, Handle);
        await _subscriber
            .PublishAsync(ctx.Route.ToString(), JsonSerializer.Serialize(ctx))
            .ConfigureAwait(false);
        return await tcs.Task;
    }

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        async Task HandleAsync(ChannelMessage m)
        {
            var messageCtx = JsonSerializer.Deserialize<MessageContext<TMessage>>(m.Message)!;

            try
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                messageCtx =
                    messageCtx with { DeliveredAt = DateTimeOffset.Now, ServiceProvider = scope.ServiceProvider };
                var result = await pipe.PassAsync<TMessage, TResult>(messageCtx, token);
                var resultCtx = new ResultContext<TResult>(messageCtx.Route, Guid.NewGuid().ToString(),
                    messageCtx.CorrelationId, DateTimeOffset.Now) { Result = result };
                
                await _subscriber
                    .PublishAsync(RedisWellKnown.ResponseMq, JsonSerializer.Serialize(resultCtx))
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                var resultCtx = new ResultContext<TResult>(messageCtx.Route, Guid.NewGuid().ToString(),
                    messageCtx.CorrelationId, DateTimeOffset.Now) { Exception = e };
                await _subscriber
                    .PublishAsync(RedisWellKnown.ResponseMq, JsonSerializer.Serialize(resultCtx))
                    .ConfigureAwait(false);
                throw;
            }
        }

        var route = Route.For<TMessage, TResult>(routingKey);
        
        var channelMq = await _subscriber.SubscribeAsync(route.ToString()).ConfigureAwait(false);
        channelMq.OnMessage(HandleAsync);
        var pipeConnection = new PipeConnection<IReqPipe>(route, pipe, async p =>
        {
            if (_pipeConnections.TryRemove(p, out _)) await channelMq.UnsubscribeAsync();
        });
        _pipeConnections.TryAdd(pipeConnection, pipeConnection);
        return pipeConnection;
    }

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
        foreach (var pipeConnection in _pipeConnections.Values) await pipeConnection.DisposeAsync();
    }
}