using System.Collections.Concurrent;
using EasyNetQ;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.RabbitMq.Pipes;

internal class RabbitMqReqPipe : IConnectingReqPipe
{
    private readonly IBus _bus;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<PipeConnection<IReqPipe>, PipeConnection<IReqPipe>> _pipeConnections = new();

    public RabbitMqReqPipe(IBus bus, IServiceProvider serviceProvider)
    {
        _bus = bus;
        _serviceProvider = serviceProvider;
    }

    public async Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> ctx,
        CancellationToken token = default) =>
        await _bus.Rpc
            .RequestAsync<MessageContext<TMessage>, TResult>(ctx, c => c.WithQueueName(ctx.Route), token)
            .ConfigureAwait(false);

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        async Task<TResult> HandleAsync(MessageContext<TMessage> m, CancellationToken c)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var ctx = m with { DeliveredAt = DateTimeOffset.Now, ServiceProvider = scope.ServiceProvider };
            return await pipe.PassAsync<TMessage, TResult>(ctx, c);
        }

        var route = Route.For<TMessage, TResult>(routingKey);
        var subscription = await _bus.Rpc
            .RespondAsync<MessageContext<TMessage>, TResult>(HandleAsync, c => c.WithQueueName(route), token)
            .ConfigureAwait(false);
        var pipeConnection = new PipeConnection<IReqPipe>(route, pipe, p =>
        {
            if (_pipeConnections.TryRemove(p, out _)) subscription.Dispose();
            return ValueTask.CompletedTask;
        });
        _pipeConnections.TryAdd(pipeConnection, pipeConnection);
        return pipeConnection;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Values) await pipeConnection.DisposeAsync();
    }
}