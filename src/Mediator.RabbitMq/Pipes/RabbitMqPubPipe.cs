using System.Collections.Concurrent;
using EasyNetQ;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.RabbitMq.Pipes;

internal class RabbitMqPubPipe : IConnectingPubPipe
{
    private readonly IBus _bus;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<PipeConnection<IPubPipe>, PipeConnection<IPubPipe>> _pipeConnections = new();

    public RabbitMqPubPipe(IBus bus, IServiceProvider serviceProvider)
    {
        _bus = bus;
        _serviceProvider = serviceProvider;
    }

    public async Task PassAsync<TMessage>(MessageContext<TMessage> ctx, CancellationToken token = default) =>
        await _bus.PubSub
            .PublishAsync(ctx, c => c.WithTopic(ctx.Route), token)
            .ConfigureAwait(false);

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default)
    {
        async Task HandleAsync(MessageContext<TMessage> m, CancellationToken c)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var ctx = m with { DeliveredAt = DateTimeOffset.Now, ServiceProvider = scope.ServiceProvider };
            await pipe.PassAsync(ctx, c);
        }

        var route = Route.For<TMessage>(routingKey);
        var subscription = await _bus.PubSub
            .SubscribeAsync<MessageContext<TMessage>>(subscriptionId, HandleAsync,
                c => c.WithQueueName($"{route}:{subscriptionId}").WithTopic(route), token)
            .ConfigureAwait(false);
        var pipeConnection = new PipeConnection<IPubPipe>(route, pipe, p =>
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