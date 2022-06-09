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

internal class RedisMqReqPipe : IConnectingReqPipe
{
    private readonly ISubscriber _subscriber;
    private readonly IServiceProvider _serviceProvider;
    private readonly Lazy<Task<ChannelMessageQueue>> _responseMq;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly ConcurrentDictionary<string, Action<ChannelMessage>> _resultHandlers = new();
    private readonly ConcurrentDictionary<PipeConnection<IReqPipe>, ChannelMessageQueue> _pipeConnections = new();

    public RedisMqReqPipe(IConnectionMultiplexer multiplexer, IServiceProvider serviceProvider)
    {
        _subscriber = multiplexer.GetSubscriber();
        _serviceProvider = serviceProvider;
        _jsonSerializerOptions = new JsonSerializerOptions { Converters = { ObjectToInferredTypesConverter.Instance } };
        _responseMq = new Lazy<Task<ChannelMessageQueue>>(CreateResultMqAsync);
    }

    public async Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> ctx,
        CancellationToken token = default)
    {
        await _responseMq.Value;
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        void Handle(ChannelMessage m)
        {
            var resultCtx = JsonSerializer.Deserialize<ResultContext<TResult>>(m.Message, _jsonSerializerOptions)!;

            if (resultCtx.Result != null) tcs.TrySetResult(resultCtx.Result);
            else if (resultCtx.Exception != null) tcs.TrySetException(resultCtx.Exception);
            else tcs.TrySetException(new MessageNotProcessedException());
        }

        if (ctx.CorrelationId == null) ctx = ctx with { CorrelationId = Guid.NewGuid().ToString() };
        _resultHandlers.TryAdd(ctx.CorrelationId, Handle);
        var message = JsonSerializer.Serialize(ctx, _jsonSerializerOptions);
        var messageProperties = new PropertyBinder<MessageProperties>().Bind(ctx.ServiceProps).Build();
        await _subscriber.PublishAsync(ctx.Route.ToString(), message, messageProperties.Flags).ConfigureAwait(false);
        return await tcs.Task.ConfigureAwait(false);
    }

    public IDisposable ConnectOut<TMessage, TResult>(IReqPipe pipe, string routingKey = "")
    {
        var route = Route.For<TMessage, TResult>(routingKey);
        return ConnectPipe<TMessage, TResult>(route, pipe);
    }

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>(routingKey);
        return await ConnectPipeAsync<TMessage, TResult>(route, pipe);
    }

    private IDisposable ConnectPipe<TMessage, TResult>(Route route, IReqPipe pipe)
    {
        var channelMq = _subscriber.Subscribe(route.ToString());
        channelMq.OnMessage(m => HandleAsync<TMessage, TResult>(pipe, m));
        var pipeConnection = new PipeConnection<IReqPipe>(route, pipe, DisconnectPipe, DisconnectPipeAsync);
        _pipeConnections.TryAdd(pipeConnection, channelMq);
        return pipeConnection;
    }

    private async Task<IAsyncDisposable> ConnectPipeAsync<TMessage, TResult>(Route route, IReqPipe pipe)
    {
        var channelMq = await _subscriber.SubscribeAsync(route.ToString()).ConfigureAwait(false);
        channelMq.OnMessage(m => HandleAsync<TMessage, TResult>(pipe, m));
        var pipeConnection = new PipeConnection<IReqPipe>(route, pipe, DisconnectPipe, DisconnectPipeAsync);
        _pipeConnections.TryAdd(pipeConnection, channelMq);
        return pipeConnection;
    }

    private void DisconnectPipe(PipeConnection<IReqPipe> pipeConnection)
    {
        if (_pipeConnections.TryRemove(pipeConnection, out var c)) c.Unsubscribe();
    }

    private async ValueTask DisconnectPipeAsync(PipeConnection<IReqPipe> pipeConnection)
    {
        if (_pipeConnections.TryRemove(pipeConnection, out var c)) await c.UnsubscribeAsync().ConfigureAwait(false);
    }

    private async Task HandleAsync<TMessage, TResult>(IReqPipe pipe, ChannelMessage message)
    {
        var ctx = JsonSerializer.Deserialize<MessageContext<TMessage>>(message.Message, _jsonSerializerOptions)!;
        TResult? result = default;
        Exception? exception = default;

        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            ctx = ctx with { DeliveredAt = DateTime.Now, ServiceProvider = scope.ServiceProvider };
            result = await pipe.PassAsync<TMessage, TResult>(ctx);
        }
        catch (Exception e)
        {
            exception = e;
            throw;
        }
        finally
        {
            var resultCtx = new ResultContext<TResult>(ctx.Route, Guid.NewGuid().ToString(), ctx.CorrelationId!)
                { Result = result, Exception = exception };
            var resultMessage = JsonSerializer.Serialize(resultCtx, _jsonSerializerOptions);
            await _subscriber.PublishAsync(WellKnown.ResultMq, resultMessage).ConfigureAwait(false);
        }
    }

    private async Task<ChannelMessageQueue> CreateResultMqAsync()
    {
        void Handle(ChannelMessage m)
        {
            var jsonElement = JsonDocument.Parse(m.Message.ToString()).RootElement;
            var correlationId = jsonElement.GetProperty("CorrelationId").ToString();

            if (_resultHandlers.TryRemove(correlationId, out var handle)) handle(m);
        }

        var channelMq = await _subscriber.SubscribeAsync(WellKnown.ResultMq).ConfigureAwait(false);
        channelMq.OnMessage(Handle);
        return channelMq;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Keys) await pipeConnection.DisposeAsync();

        if (_responseMq.IsValueCreated) _responseMq.Value.Dispose();
    }
}