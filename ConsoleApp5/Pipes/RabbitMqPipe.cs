using EasyNetQ;

namespace ConsoleApp5.Pipes;

public class RabbitMqPipe : IPipe
{
    private readonly IBus _bus;

    public RabbitMqPipe(IBus bus)
    {
        _bus = bus;
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options, CancellationToken token)
    {
        await _bus.PubSub.PublishAsync(message, c => c.WithTopic(options.RoutingKey), token);
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token)
    {
        return await _bus.Rpc.RequestAsync<TMessage, TResult>(message, c => c.WithQueueName(options.RoutingKey), token);
    }
}