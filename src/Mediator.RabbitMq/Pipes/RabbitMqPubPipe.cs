using System.Collections.Concurrent;
using EasyNetQ;
using EasyNetQ.Topology;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.RabbitMq.Pipes;

internal class RabbitMqPubPipe : IConnectingPubPipe
{
    private readonly IBus _bus;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<PipeConnection<IPubPipe>, IDisposable> _pipeConnections = new();

    public RabbitMqPubPipe(IBus bus, IServiceProvider serviceProvider)
    {
        _bus = bus;
        _serviceProvider = serviceProvider;
    }

    public async Task PassAsync<TMessage>(MessageContext<TMessage> ctx, CancellationToken token = default)
    {
        var exchange = await DeclareExchangeAsync(ctx.Route, token);
        var message = new Message<MessageContext<TMessage>>(ctx);
        await _bus.Advanced.PublishAsync(exchange, ctx.Route, false, message, token).ConfigureAwait(false);
    }

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default)
    {
        var route = Route.For<TMessage>(routingKey);
        var queue = await DeclareQueueAsync(route, subscriptionId, token);
        var pipeConnection = ConnectPipe<TMessage>(route, pipe, queue);
        return pipeConnection;
    }

    private PipeConnection<IPubPipe> ConnectPipe<TMessage>(Route route, IPubPipe pipe, IQueue queue)
    {
        async Task HandleAsync(IMessage<MessageContext<TMessage>> m, MessageReceivedInfo i)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var ctx = m.Body with { DeliveredAt = DateTimeOffset.Now, ServiceProvider = scope.ServiceProvider };
            await pipe.PassAsync(ctx);
        }

        var subscription = _bus.Advanced.Consume<MessageContext<TMessage>>(queue, HandleAsync);
        var pipeConnection = new PipeConnection<IPubPipe>(route, pipe, DisconnectPipe);
        _pipeConnections.TryAdd(pipeConnection, subscription);
        return pipeConnection;
    }

    private ValueTask DisconnectPipe(PipeConnection<IPubPipe> p)
    {
        if (_pipeConnections.TryRemove(p, out var s)) s.Dispose();
        return new ValueTask();
    }

    private async Task<IExchange> DeclareExchangeAsync(Route route, CancellationToken token = default)
    {
        var exchange = await _bus.Advanced
            .ExchangeDeclareAsync(route, ExchangeType.Direct, cancellationToken: token)
            .ConfigureAwait(false);
        return exchange;
    }

    private async Task<IQueue> DeclareQueueAsync(Route route, string subscriptionId, CancellationToken token = default)
    {
        var exchange = await DeclareExchangeAsync(route, token);
        var queue = await _bus.Advanced.QueueDeclareAsync($"{route}:{subscriptionId}", token).ConfigureAwait(false);
        await _bus.Advanced.BindAsync(exchange, queue, route, token).ConfigureAwait(false);
        return queue;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Keys) await pipeConnection.DisposeAsync();
    }
}