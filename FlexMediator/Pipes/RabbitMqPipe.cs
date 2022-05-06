using EasyNetQ;
using FlexMediator.Utils;

namespace FlexMediator.Pipes;

public class RabbitMqPipe : IPipe, IPipeConnector
{
    private readonly IBus _bus;
    private readonly IPipeConnector _pipeConnector;

    public RabbitMqPipe(IBus bus)
    {
        _bus = bus;
        _pipeConnector = new RabbitMqPipeConnector(bus);
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

    public Task<PipeConnection> Out<TMessage>(IPipe pipe, string routingKey = "") =>
        _pipeConnector.Out<TMessage>(pipe, routingKey);

    public Task<PipeConnection> Out<TMessage, TResult>(IPipe pipe, string routingKey = "") =>
        _pipeConnector.Out<TMessage, TResult>(pipe, routingKey);
}