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
    private readonly Lazy<Task<IDisposable>> _responseMq;
    private readonly ConcurrentDictionary<string, Action<IMessage>> _responseHandlers = new();
    private readonly ConcurrentDictionary<PipeConnection<IReqPipe>, PipeConnection<IReqPipe>> _pipeConnections = new();

    public RabbitMqReqPipe(IBus bus, IServiceProvider serviceProvider)
    {
        _bus = bus;
        _serviceProvider = serviceProvider;
        _responseMq = new Lazy<Task<IDisposable>>(CreateResponseMq);
    }

    public async Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> ctx,
        CancellationToken token = default)
    {
        await _responseMq.Value;

        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        void Handle(IMessage m)
        {
            var resultCtx = (ResultContext<TResult>)m.GetBody();

            if (resultCtx.Result != null) tcs.TrySetResult(resultCtx.Result);
            else if (resultCtx.Exception != null) tcs.TrySetException(resultCtx.Exception);
            else tcs.TrySetException(new InvalidOperationException("Message was not be processed"));
        }

        _responseHandlers.TryAdd(ctx.CorrelationId, Handle);

        var exchange = await _bus.Advanced
            .ExchangeDeclareAsync(ctx.Route, ExchangeType.Direct, cancellationToken: token)
            .ConfigureAwait(false);
        var message = new Message<MessageContext<TMessage>>(ctx);
        await _bus.Advanced.PublishAsync(exchange, ctx.Route, false, message, token).ConfigureAwait(false);
        return await tcs.Task;
    }

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        async Task HandleAsync(MessageContext<TMessage> m, CancellationToken c)
        {
            var exchange = await _bus.Advanced
                .ExchangeDeclareAsync(RabbitMqWellKnown.ResponseMq, ExchangeType.Direct, cancellationToken: c)
                .ConfigureAwait(false);
            var queue = await _bus.Advanced.QueueDeclareAsync(RabbitMqWellKnown.ResponseMq, c)
                .ConfigureAwait(false);
            await _bus.Advanced.BindAsync(exchange, queue, "", default, token).ConfigureAwait(false);
            
            try
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                m = m with { DeliveredAt = DateTimeOffset.Now, ServiceProvider = scope.ServiceProvider };
                var result = await pipe.PassAsync<TMessage, TResult>(m, token);
                var resultCtx = new ResultContext<TResult>(m.Route, Guid.NewGuid().ToString(),
                    m.CorrelationId, DateTimeOffset.Now) { Result = result };

                var message = new Message<ResultContext<TResult>>(resultCtx);
                await _bus.Advanced.PublishAsync(exchange, RabbitMqWellKnown.ResponseMq, false, message, token)
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                var resultCtx = new ResultContext<TResult>(m.Route, Guid.NewGuid().ToString(),
                    m.CorrelationId, DateTimeOffset.Now) { Exception = e };
                
                var message = new Message<ResultContext<TResult>>(resultCtx);
                await _bus.Advanced.PublishAsync(exchange, RabbitMqWellKnown.ResponseMq, false, message, token)
                    .ConfigureAwait(false);
                throw;
            }
        }

        var route = Route.For<TMessage, TResult>(routingKey);

        var exchange = await _bus.Advanced
            .ExchangeDeclareAsync(route, ExchangeType.Direct, cancellationToken: token)
            .ConfigureAwait(false);
        var queue = await _bus.Advanced.QueueDeclareAsync(route, token).ConfigureAwait(false);
        await _bus.Advanced.BindAsync(exchange, queue, route, token).ConfigureAwait(false);

        var subscription = _bus.Advanced.Consume<MessageContext<TMessage>>(queue, (m, _) => HandleAsync(m.Body, token));
        var pipeConnection = new PipeConnection<IReqPipe>(route, pipe, p =>
        {
            if (_pipeConnections.TryRemove(p, out _)) subscription.Dispose();
            return ValueTask.CompletedTask;
        });
        _pipeConnections.TryAdd(pipeConnection, pipeConnection);
        return pipeConnection;
    }

    private async Task<IDisposable> CreateResponseMq()
    {
        void Handle(IMessage<dynamic> m)
        {
            var correlationId = m.Body.CorrelationId;

            if (_responseHandlers.TryRemove(correlationId, out Action<IMessage> handle)) handle(m);
        }

        var exchange = await _bus.Advanced
            .ExchangeDeclareAsync(RabbitMqWellKnown.ResponseMq, ExchangeType.Direct)
            .ConfigureAwait(false);
        var queue = await _bus.Advanced.QueueDeclareAsync(RabbitMqWellKnown.ResponseMq).ConfigureAwait(false);
        await _bus.Advanced.BindAsync(exchange, queue, "", default).ConfigureAwait(false);

        var subscription = _bus.Advanced.Consume<dynamic>(queue, (m, _) => Handle(m));
        return subscription;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Values) await pipeConnection.DisposeAsync();
    }
}