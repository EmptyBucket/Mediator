using EasyNetQ;
using EasyNetQ.Topology;
using Mediator.Pipes.Utils;

namespace Mediator.RabbitMq.Utils;

internal static class BusExtensions
{
    public static IExchange DeclareExchange(this IAdvancedBus bus, Route route)
    {
        var exchange = bus.ExchangeDeclare(route, ExchangeType.Direct);
        return exchange;
    }

    public static async Task<IExchange> DeclareExchangeAsync(this IAdvancedBus bus, Route route,
        CancellationToken token = default)
    {
        var exchange = await bus
            .ExchangeDeclareAsync(route, ExchangeType.Direct, cancellationToken: token)
            .ConfigureAwait(false);
        return exchange;
    }

    public static IQueue DeclareBoundQueue(this IAdvancedBus bus, Route route)
    {
        var exchange = bus.DeclareExchange(route);
        var queue = bus.QueueDeclare(route);
        bus.Bind(exchange, queue, route);
        return queue;
    }

    public static IQueue DeclareBoundQueue(this IAdvancedBus bus, Route route, string subscriptionId)
    {
        var exchange = bus.DeclareExchange(route);
        var queue = bus.QueueDeclare($"{route}:{subscriptionId}");
        bus.Bind(exchange, queue, route);
        return queue;
    }

    public static async Task<IQueue> DeclareBoundQueueAsync(this IAdvancedBus bus, Route route,
        CancellationToken token = default)
    {
        var exchange = await bus.DeclareExchangeAsync(route, token);
        var queue = await bus.QueueDeclareAsync(route, token).ConfigureAwait(false);
        await bus.BindAsync(exchange, queue, route, token).ConfigureAwait(false);
        return queue;
    }

    public static async Task<IQueue> DeclareBoundQueueAsync(this IAdvancedBus bus, Route route, string subscriptionId,
        CancellationToken token = default)
    {
        var exchange = await bus.DeclareExchangeAsync(route, token);
        var queue = await bus.QueueDeclareAsync($"{route}:{subscriptionId}", token).ConfigureAwait(false);
        await bus.BindAsync(exchange, queue, route, token).ConfigureAwait(false);
        return queue;
    }

    public static async Task<IExchange> DeclareResultExchangeAsync(this IAdvancedBus bus,
        CancellationToken token = default)
    {
        var exchange = await bus
            .ExchangeDeclareAsync(WellKnown.ResultMq, ExchangeType.Fanout, cancellationToken: token)
            .ConfigureAwait(false);
        return exchange;
    }

    public static async Task<IQueue> DeclareBoundResultQueueAsync(this IAdvancedBus bus,
        CancellationToken token = default)
    {
        var exchange = await bus.DeclareResultExchangeAsync(token);
        var queue = await bus.QueueDeclareAsync(WellKnown.ResultMq, token).ConfigureAwait(false);
        await bus.BindAsync(exchange, queue, string.Empty, token).ConfigureAwait(false);
        return queue;
    }
}