using EasyNetQ;
using FlexMediator;
using FlexMediator.Pipes;

namespace FlexMediatorRabbit;

public class RabbitMqPipe : IPipe
{
    private readonly IBus _bus;

    public RabbitMqPipe(IBus bus)
    {
        _bus = bus;
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options, CancellationToken token)
    {
        var route = new Route(typeof(TMessage), options.RoutingKey);

        await _bus.PubSub.PublishAsync(message, c => c.WithTopic(route.ToString()), token);
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token)
    {
        var route = new Route(typeof(TMessage), options.RoutingKey, typeof(TResult));

        return await _bus.Rpc.RequestAsync<TMessage, TResult>(message, c => c.WithQueueName(route.ToString()), token);
    }
}