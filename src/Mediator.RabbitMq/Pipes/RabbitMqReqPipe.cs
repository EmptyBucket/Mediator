using System.Collections.Concurrent;
using EasyNetQ;
using EasyNetQ.Topology;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.RabbitMq.Pipes;

internal class RabbitMqReqPipe : IConnectingReqPipe
{
    private readonly IBus _bus;
    private readonly IServiceProvider _serviceProvider;
    private readonly Lazy<Task<IDisposable>> _resultMq;
    private readonly ConcurrentDictionary<string, Action<IMessage, MessageReceivedInfo>> _resultHandlers = new();
    private readonly ConcurrentDictionary<PipeConnection<IReqPipe>, IDisposable> _pipeConnections = new();

    public RabbitMqReqPipe(IBus bus, IServiceProvider serviceProvider)
    {
        _bus = bus;
        _serviceProvider = serviceProvider;
        _resultMq = new Lazy<Task<IDisposable>>(CreateResultMq);
    }

    public async Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> ctx,
        CancellationToken token = default)
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

        var exchange = await DeclareMessageExchangeAsync(ctx.Route, token);
        _resultHandlers.TryAdd(ctx.CorrelationId, Handle);
        var message = new Message<MessageContext<TMessage>>(ctx);
        await _bus.Advanced.PublishAsync(exchange, ctx.Route, false, message, token).ConfigureAwait(false);
        return await tcs.Task;
    }

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>(routingKey);
        var queue = await DeclareMessageQueueAsync(route, token);
        var pipeConnection = ConnectPipe<TMessage, TResult>(route, pipe, queue);
        return pipeConnection;
    }

    private PipeConnection<IReqPipe> ConnectPipe<TMessage, TResult>(Route route, IReqPipe pipe, IQueue queue)
    {
        async Task HandleAsync(IMessage<MessageContext<TMessage>> m, MessageReceivedInfo i)
        {
            var exchange = await DeclareResultExchangeAsync();
            var ctx = m.Body;
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
                var message = new Message<ResultContext<TResult>>(resultCtx);
                await _bus.Advanced.PublishAsync(exchange, WellKnown.ResponseMq, false, message).ConfigureAwait(false);
            }
        }

        var subscription = _bus.Advanced.Consume<MessageContext<TMessage>>(queue, HandleAsync);
        var pipeConnection = new PipeConnection<IReqPipe>(route, pipe, DisconnectPipe);
        _pipeConnections.TryAdd(pipeConnection, subscription);
        return pipeConnection;
    }

    private ValueTask DisconnectPipe(PipeConnection<IReqPipe> p)
    {
        if (_pipeConnections.TryRemove(p, out var s)) s.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task<IDisposable> CreateResultMq()
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

    private async Task<IExchange> DeclareResultExchangeAsync(CancellationToken token = default)
    {
        var exchange = await _bus.Advanced
            .ExchangeDeclareAsync(WellKnown.ResponseMq, ExchangeType.Fanout, cancellationToken: token)
            .ConfigureAwait(false);
        return exchange;
    }

    private async Task<IQueue> DeclareResultQueueAsync(CancellationToken token = default)
    {
        var exchange = await DeclareResultExchangeAsync(token);
        var queue = await _bus.Advanced.QueueDeclareAsync(WellKnown.ResponseMq, token).ConfigureAwait(false);
        await _bus.Advanced.BindAsync(exchange, queue, "", default, token).ConfigureAwait(false);
        return queue;
    }

    private async Task<IExchange> DeclareMessageExchangeAsync(Route route, CancellationToken token = default)
    {
        var exchange = await _bus.Advanced
            .ExchangeDeclareAsync(route, ExchangeType.Direct, cancellationToken: token)
            .ConfigureAwait(false);
        return exchange;
    }

    private async Task<IQueue> DeclareMessageQueueAsync(Route route, CancellationToken token = default)
    {
        var exchange = await DeclareMessageExchangeAsync(route, token);
        var queue = await _bus.Advanced.QueueDeclareAsync(route, token).ConfigureAwait(false);
        await _bus.Advanced.BindAsync(exchange, queue, route, token).ConfigureAwait(false);
        return queue;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Keys) await pipeConnection.DisposeAsync();
    }
}