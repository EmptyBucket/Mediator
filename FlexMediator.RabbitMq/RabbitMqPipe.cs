using EasyNetQ;
using FlexMediator;
using FlexMediator.Pipes;

namespace FlexMediatorRabbit;

public class RabbitMqPipe : IPipe, IPipeBinder
{
    private readonly IBus _bus;
    private IPipeBinder _pipeBinder;

    public RabbitMqPipe(IBus bus)
    {
        _bus = bus;
        _pipeBinder = new RabbitMqPipeBinder(bus);
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

    public Task<PipeBind> Bind<TMessage>(IPipe pipe, string routingKey = "")
    {
        return _pipeBinder.Bind<TMessage>(pipe, routingKey);
    }

    public Task<PipeBind> Bind<TMessage, TResult>(IPipe pipe, string routingKey = "")
    {
        return _pipeBinder.Bind<TMessage, TResult>(pipe, routingKey);
    }
}