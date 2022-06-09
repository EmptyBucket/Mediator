using System.Collections.Concurrent;
using EasyNetQ;
using EasyNetQ.Topology;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.Utils;
using Mediator.RabbitMq.Utils;
using Mediator.Utils;
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
        _resultMq = new Lazy<Task<IDisposable>>(CreateResultMqAsync);
    }

    public async Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> ctx,
        CancellationToken token = default)
    {
        await _resultMq.Value;
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        void Handle(IMessage m, MessageReceivedInfo i)
        {
            var resultCtx = (ResultContext<TResult>)m.GetBody();

            if (resultCtx.Result != null) tcs.TrySetResult(resultCtx.Result);
            else if (resultCtx.Exception != null) tcs.TrySetException(resultCtx.Exception);
            else tcs.TrySetException(new MessageNotProcessedException());
        }

        var exchange = await _bus.Advanced.DeclareExchangeAsync(ctx.Route, token);
        if (ctx.CorrelationId == null) ctx = ctx with { CorrelationId = Guid.NewGuid().ToString() };
        _resultHandlers.TryAdd(ctx.CorrelationId, Handle);
        var messageProperties = new PropertyBinder<MessageProperties>().Bind(ctx.ServiceProps).Build();
        var message = new Message<MessageContext<TMessage>>(ctx, messageProperties);
        await _bus.Advanced.PublishAsync(exchange, ctx.Route, false, message, token).ConfigureAwait(false);
        return await tcs.Task.ConfigureAwait(false);
    }

    public IDisposable ConnectOut<TMessage, TResult>(IReqPipe pipe, string routingKey = "")
    {
        var route = Route.For<TMessage, TResult>(routingKey);
        var queue = _bus.Advanced.DeclareBoundQueue(route);
        var pipeConnection = ConnectPipe<TMessage, TResult>(route, pipe, queue);
        return pipeConnection;
    }

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>(routingKey);
        var queue = await _bus.Advanced.DeclareBoundQueueAsync(route, token);
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
        var exchange = await _bus.Advanced.DeclareResultExchangeAsync();
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

        var queue = await _bus.Advanced.DeclareBoundResultQueueAsync();
        var subscription = _bus.Advanced.Consume<dynamic>(queue, Handle);
        return subscription;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Keys) await pipeConnection.DisposeAsync();

        if (_resultMq.IsValueCreated) _resultMq.Value.Dispose();
    }
}