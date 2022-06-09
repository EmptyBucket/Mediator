using System.Collections.Concurrent;
using EasyNetQ;
using EasyNetQ.Topology;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.Utils;
using Mediator.Utils;
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
        var messageProperties = new PropertyBinder<MessageProperties>().Bind(ctx.ServiceProps).Build();
        var message = new Message<MessageContext<TMessage>>(ctx, messageProperties);
        await _bus.Advanced.PublishAsync(exchange, ctx.Route, false, message, token).ConfigureAwait(false);
    }

    public IDisposable ConnectOut<TMessage>(IPubPipe pipe, string routingKey = "", string subscriptionId = "")
    {
        var route = Route.For<TMessage>(routingKey);
        var queue = DeclareQueue(route, subscriptionId);
        var pipeConnection = ConnectPipe<TMessage>(route, pipe, queue);
        return pipeConnection;
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
        var subscription = _bus.Advanced.Consume<MessageContext<TMessage>>(queue, (m, _) => HandleAsync(pipe, m));
        var pipeConnection = new PipeConnection<IPubPipe>(route, pipe, DisconnectPipe);
        _pipeConnections.TryAdd(pipeConnection, subscription);
        return pipeConnection;
    }

    private void DisconnectPipe(PipeConnection<IPubPipe> p)
    {
        if (_pipeConnections.TryRemove(p, out var s)) s.Dispose();
    }

    private async Task HandleAsync<TMessage>(IPubPipe pipe, IMessage<MessageContext<TMessage>> message)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var ctx = message.Body with { DeliveredAt = DateTime.Now, ServiceProvider = scope.ServiceProvider };
        await pipe.PassAsync(ctx);
    }

    private IExchange DeclareExchange(Route route)
    {
        var exchange = _bus.Advanced.ExchangeDeclare(route, ExchangeType.Direct);
        return exchange;
    }

    private async Task<IExchange> DeclareExchangeAsync(Route route, CancellationToken token = default)
    {
        var exchange = await _bus.Advanced
            .ExchangeDeclareAsync(route, ExchangeType.Direct, cancellationToken: token)
            .ConfigureAwait(false);
        return exchange;
    }

    private IQueue DeclareQueue(Route route, string subscriptionId)
    {
        var exchange = DeclareExchange(route);
        var queue = _bus.Advanced.QueueDeclare($"{route}:{subscriptionId}");
        _bus.Advanced.Bind(exchange, queue, route);
        return queue;
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