using EasyNetQ;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.PublishSubscribe;
using Mediator.Pipes.RequestResponse;

namespace Mediator.RabbitMq.Pipes;

public class RabbitMqPipe : IConnectingPipe
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

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default) =>
        _pipeConnector.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionId, token);

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        _pipeConnector.ConnectOutAsync<TMessage, TResult>(pipe, routingKey, token);

    public ValueTask DisposeAsync() => _pipeConnector.DisposeAsync();
}