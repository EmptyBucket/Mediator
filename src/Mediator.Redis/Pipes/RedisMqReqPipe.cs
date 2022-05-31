// MIT License
// 
// Copyright (c) 2022 Alexey Politov
// https://github.com/EmptyBucket/Mediator
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

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
    private readonly ConcurrentDictionary<string, Action<ChannelMessage>> _resultHandlers = new();
    private readonly ConcurrentDictionary<PipeConnection<IReqPipe>, ChannelMessageQueue> _pipeConnections = new();

    public RedisMqReqPipe(IConnectionMultiplexer multiplexer, IServiceProvider serviceProvider)
    {
        _subscriber = multiplexer.GetSubscriber();
        _serviceProvider = serviceProvider;
        _responseMq = new Lazy<Task<ChannelMessageQueue>>(CreateResultMq);
    }

    public async Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> ctx,
        CancellationToken token = default)
    {
        await _responseMq.Value;
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        void Handle(ChannelMessage m)
        {
            // ReSharper disable once VariableHidesOuterVariable
            var ctx = JsonSerializer.Deserialize<ResultContext<TResult>>(m.Message)!;

            if (ctx.Result != null) tcs.TrySetResult(ctx.Result);
            else if (ctx.Exception != null) tcs.TrySetException(ctx.Exception);
            else tcs.TrySetException(new InvalidOperationException("Message was not be processed"));
        }

        _resultHandlers.TryAdd(ctx.CorrelationId, Handle);
        var redisValue = JsonSerializer.Serialize(ctx);
        await _subscriber.PublishAsync(ctx.Route.ToString(), redisValue).ConfigureAwait(false);
        return await tcs.Task;
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

    private void DisconnectPipe(PipeConnection<IReqPipe> p)
    {
        if (_pipeConnections.TryRemove(p, out var c)) c.Unsubscribe();
    }

    private async ValueTask DisconnectPipeAsync(PipeConnection<IReqPipe> p)
    {
        if (_pipeConnections.TryRemove(p, out var c)) await c.UnsubscribeAsync();
    }

    private async Task HandleAsync<TMessage, TResult>(IReqPipe pipe, ChannelMessage m)
    {
        var ctx = JsonSerializer.Deserialize<MessageContext<TMessage>>(m.Message)!;
        TResult? result = default;
        Exception? exception = default;

        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            ctx = ctx with { DeliveredAt = DateTimeOffset.Now, ServiceProvider = scope.ServiceProvider };
            result = await pipe.PassAsync<TMessage, TResult>(ctx);
        }
        catch (Exception e)
        {
            exception = e;
            throw;
        }
        finally
        {
            var messageId = Guid.NewGuid().ToString();
            var resultCtx = new ResultContext<TResult>(ctx.Route, messageId, ctx.CorrelationId, DateTimeOffset.Now)
                { Result = result, Exception = exception };
            var redisValue = JsonSerializer.Serialize(resultCtx);
            await _subscriber.PublishAsync(WellKnown.ResultMq, redisValue).ConfigureAwait(false);
        }
    }

    private async Task<ChannelMessageQueue> CreateResultMq()
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