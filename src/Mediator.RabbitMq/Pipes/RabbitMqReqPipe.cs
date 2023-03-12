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
using EasyNetQ;
using EasyNetQ.Topology;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.Utils;
using Mediator.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.RabbitMq.Pipes;

internal class RabbitMqReqPipe : IConnectingReqPipe
{
    private int _isDisposed;
    private readonly IBus _bus;
    private readonly IServiceProvider _serviceProvider;
    private readonly Lazy<Task<IDisposable>> _resultMq;
    private readonly ConcurrentDictionary<string, Action<IMessage, MessageReceivedInfo>> _resultHandlers = new();
    private readonly ConcurrentDictionary<PipeConnection<IReqPipe>, IDisposable> _pipeConnections = new();

    public RabbitMqReqPipe(IBus bus, IServiceProvider serviceProvider)
    {
        _bus = bus;
        _serviceProvider = serviceProvider;
        _resultMq = new Lazy<Task<IDisposable>>(CreateResultMqAsync);
    }

    public async Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> ctx,
        CancellationToken cancellationToken = default)
    {
        await _resultMq.Value;
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        void Handle(IMessage m, MessageReceivedInfo i)
        {
            // ReSharper disable once VariableHidesOuterVariable
            var ctx = (ResultContext<TResult>)m.GetBody();

            if (ctx.Result != null) tcs.TrySetResult(ctx.Result);
            else if (ctx.Exception != null) tcs.TrySetException(ctx.Exception);
            else tcs.TrySetException(new InvalidOperationException("Message was not be processed"));
        }

        var exchange = await DeclareMessageExchangeAsync(ctx.Route, cancellationToken);
        if (ctx.CorrelationId == null) ctx = ctx with { CorrelationId = Guid.NewGuid().ToString() };
        _resultHandlers.TryAdd(ctx.CorrelationId, Handle);
        var messageProperties = new PropertyBinder<MessageProperties>().Bind(ctx.ServiceProps).Build();
        var message = new Message<MessageContext<TMessage>>(ctx, messageProperties);
        await _bus.Advanced.PublishAsync(exchange, ctx.Route, false, message, cancellationToken).ConfigureAwait(false);
        return await tcs.Task.ConfigureAwait(false);
    }

    public IDisposable ConnectOut<TMessage, TResult>(IReqPipe pipe, string routingKey = "")
    {
        var route = Route.For<TMessage, TResult>(routingKey);
        var queue = DeclareMessageQueue(route);
        var pipeConnection = ConnectPipe<TMessage, TResult>(route, pipe, queue);
        return pipeConnection;
    }

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken cancellationToken = default)
    {
        var route = Route.For<TMessage, TResult>(routingKey);
        var queue = await DeclareMessageQueueAsync(route, cancellationToken);
        var pipeConnection = ConnectPipe<TMessage, TResult>(route, pipe, queue);
        return pipeConnection;
    }

    private PipeConnection<IReqPipe> ConnectPipe<TMessage, TResult>(Route route, IReqPipe pipe, IQueue queue)
    {
        var subscription =
            _bus.Advanced.Consume<MessageContext<TMessage>>(queue, (m, _) => HandleAsync<TMessage, TResult>(pipe, m));
        var pipeConnection = new PipeConnection<IReqPipe>(route, pipe, DisconnectPipe);
        _pipeConnections.TryAdd(pipeConnection, subscription);
        return pipeConnection;
    }

    private void DisconnectPipe(PipeConnection<IReqPipe> pipeConnection)
    {
        if (_pipeConnections.TryRemove(pipeConnection, out var s)) s.Dispose();
    }

    private async Task HandleAsync<TMessage, TResult>(IReqPipe pipe, IMessage<MessageContext<TMessage>> message)
    {
        var exchange = await DeclareResultExchangeAsync();
        var ctx = message.Body;
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
            var resultMessage = new Message<ResultContext<TResult>>(resultCtx);
            await _bus.Advanced.PublishAsync(exchange, WellKnown.ResultMq, false, resultMessage).ConfigureAwait(false);
        }
    }

    private async Task<IDisposable> CreateResultMqAsync()
    {
        void Handle(IMessage<dynamic> m, MessageReceivedInfo i)
        {
            var correlationId = m.Body.CorrelationId;

            if (_resultHandlers.TryRemove(correlationId, out Action<IMessage, MessageReceivedInfo> handle))
                handle(m, i);
        }

        var queue = await DeclareResultQueueAsync();
        var subscription = _bus.Advanced.Consume<dynamic>(queue, Handle);
        return subscription;
    }

    private async Task<IExchange> DeclareResultExchangeAsync(CancellationToken cancellationToken = default)
    {
        var exchange = await _bus.Advanced
            .ExchangeDeclareAsync(WellKnown.ResultMq, ExchangeType.Fanout, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return exchange;
    }

    private async Task<IQueue> DeclareResultQueueAsync(CancellationToken cancellationToken = default)
    {
        var exchange = await DeclareResultExchangeAsync(cancellationToken);
        var queue = await _bus.Advanced.QueueDeclareAsync(WellKnown.ResultMq, cancellationToken).ConfigureAwait(false);
        await _bus.Advanced.BindAsync(exchange, queue, string.Empty, cancellationToken).ConfigureAwait(false);
        return queue;
    }

    private IExchange DeclareMessageExchange(Route route)
    {
        var exchange = _bus.Advanced.ExchangeDeclare(route, ExchangeType.Direct);
        return exchange;
    }

    private async Task<IExchange> DeclareMessageExchangeAsync(Route route,
        CancellationToken cancellationToken = default)
    {
        var exchange = await _bus.Advanced
            .ExchangeDeclareAsync(route, ExchangeType.Direct, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return exchange;
    }

    private IQueue DeclareMessageQueue(Route route)
    {
        var exchange = DeclareMessageExchange(route);
        var queue = _bus.Advanced.QueueDeclare(route);
        _bus.Advanced.Bind(exchange, queue, route);
        return queue;
    }

    private async Task<IQueue> DeclareMessageQueueAsync(Route route, CancellationToken cancellationToken = default)
    {
        var exchange = await DeclareMessageExchangeAsync(route, cancellationToken);
        var queue = await _bus.Advanced.QueueDeclareAsync(route, cancellationToken).ConfigureAwait(false);
        await _bus.Advanced.BindAsync(exchange, queue, route, cancellationToken).ConfigureAwait(false);
        return queue;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) return;

        foreach (var pipeConnection in _pipeConnections.Keys) await pipeConnection.DisposeAsync();

        if (_resultMq.IsValueCreated) _resultMq.Value.Dispose();
    }
}