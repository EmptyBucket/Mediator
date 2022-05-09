using EasyNetQ;
using FlexMediator.Utils;

namespace FlexMediator.Pipes;

public class RabbitMqPipe : IPipe, IPipeConnector
{
    private readonly IBus _bus;
    private readonly IPipeConnector _pipeConnector;

    public RabbitMqPipe(IBus bus, IServiceProvider serviceProvider)
    {
        _bus = bus;
        _pipeConnector = new RabbitMqPipeConnector(bus, serviceProvider);
    }

    public async Task PassAsync<TMessage>(TMessage message, MessageContext context,
        CancellationToken token = default)
    {
        var route = Route.For<TMessage>(context.RoutingKey);
        await _bus.PubSub.PublishAsync(message, c => c.WithTopic(route), token)
            .ConfigureAwait(false);
    }

    public async Task<TResult> PassAsync<TMessage, TResult>(TMessage message, MessageContext context,
        CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>(context.RoutingKey);
        return await _bus.Rpc.RequestAsync<TMessage, TResult>(message, c => c.WithQueueName(route), token)
            .ConfigureAwait(false);
    }

    public Task<PipeConnection> ConnectInAsync<TMessage>(IPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        _pipeConnector.ConnectInAsync<TMessage>(pipe, routingKey, token);

    public Task<PipeConnection> ConnectInAsync<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        _pipeConnector.ConnectInAsync<TMessage, TResult>(pipe, routingKey, token);

    public ValueTask DisposeAsync() => _pipeConnector.DisposeAsync();
}