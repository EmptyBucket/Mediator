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
using Mediator.Redis.Utils;
using Mediator.Utils;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Mediator.Redis.Pipes;

internal class RedisMqReqPipe : IMulticastReqPipe
{
    private int _isDisposed;
    private readonly ISubscriber _subscriber;
    private readonly IServiceProvider _serviceProvider;
    private readonly Lazy<Task<ChannelMessageQueue>> _resultMq;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly ConcurrentDictionary<string, Action<ChannelMessage>> _resultHandlers = new();
    private readonly ConcurrentDictionary<PipeConnection<IReqPipe>, ChannelMessageQueue> _pipeConnections = new();

    public RedisMqReqPipe(IConnectionMultiplexer multiplexer, IServiceProvider serviceProvider)
    {
        _subscriber = multiplexer.GetSubscriber();
        _serviceProvider = serviceProvider;
        _jsonSerializerOptions = new JsonSerializerOptions { Converters = { ObjectToInferredTypesConverter.Instance } };
        _resultMq = new Lazy<Task<ChannelMessageQueue>>(CreateResultMqAsync);
    }

    /// <inheritdoc />
    public async Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> ctx,
        CancellationToken cancellationToken = default)
    {
        await _resultMq.Value;
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        void Handle(ChannelMessage m)
        {
            // ReSharper disable once VariableHidesOuterVariable
            var ctx = JsonSerializer.Deserialize<ResultContext<TResult>>(m.Message, _jsonSerializerOptions)!;

            if (ctx.ExceptionMessage == null && ctx.Result != null) tcs.TrySetResult(ctx.Result);
            else tcs.TrySetException(new MessageUnhandledException(ctx.ExceptionMessage ?? string.Empty));
        }

        if (ctx.CorrelationId == null) ctx = ctx with { CorrelationId = Guid.NewGuid().ToString() };
        _resultHandlers.TryAdd(ctx.CorrelationId, Handle);
        var message = JsonSerializer.Serialize(ctx, _jsonSerializerOptions);
        var messageProperties = new PropertyBinder<MessageProperties>().Bind(ctx.ServiceProps).Build();
        await _subscriber.PublishAsync(ctx.Route.ToString(), message, messageProperties.Flags).ConfigureAwait(false);
        return await tcs.Task.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IEnumerable<PipeConnection<IReqPipe>> GetReqConnections() => _pipeConnections.Keys;

    /// <inheritdoc />
    public PipeConnection<IReqPipe> ConnectOut<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        string connectionName = "")
    {
        var route = Route.For<TMessage, TResult>(routingKey);
        return ConnectPipe<TMessage, TResult>(connectionName, route, pipe);
    }

    /// <inheritdoc />
    public async Task<PipeConnection<IReqPipe>> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe,
        string routingKey = "", string connectionName = "", CancellationToken cancellationToken = default)
    {
        var route = Route.For<TMessage, TResult>(routingKey);
        return await ConnectPipeAsync<TMessage, TResult>(connectionName, route, pipe);
    }

    private PipeConnection<IReqPipe> ConnectPipe<TMessage, TResult>(string connectionName, Route route, IReqPipe pipe)
    {
        var channelMq = _subscriber.Subscribe(route.ToString());
        channelMq.OnMessage(m => HandleAsync<TMessage, TResult>(pipe, m));
        var pipeConnection = new PipeConnection<IReqPipe>(connectionName, route, pipe, DisconnectPipe,
            DisconnectPipeAsync);
        _pipeConnections.TryAdd(pipeConnection, channelMq);
        return pipeConnection;
    }

    private async Task<PipeConnection<IReqPipe>> ConnectPipeAsync<TMessage, TResult>(string connectionName, Route route,
        IReqPipe pipe)
    {
        var channelMq = await _subscriber.SubscribeAsync(route.ToString()).ConfigureAwait(false);
        channelMq.OnMessage(m => HandleAsync<TMessage, TResult>(pipe, m));
        var pipeConnection = new PipeConnection<IReqPipe>(connectionName, route, pipe, DisconnectPipe,
            DisconnectPipeAsync);
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
                { Result = result, ExceptionMessage = exception?.Message };
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
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) return;

        foreach (var pipeConnection in _pipeConnections.Keys) await pipeConnection.DisposeAsync();

        if (_resultMq.IsValueCreated) _resultMq.Value.Dispose();
    }
}