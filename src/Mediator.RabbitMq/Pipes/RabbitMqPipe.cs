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

    public async Task PassAsync<TMessage>(MessageContext<TMessage> context, CancellationToken token = default) =>
        await _bus.PubSub
            .PublishAsync(context, c => c.WithTopic(context.Route), token)
            .ConfigureAwait(false);

    public async Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> context,
        CancellationToken token = default) =>
        await _bus.Rpc
            .RequestAsync<MessageContext<TMessage>, TResult>(context, c => c.WithQueueName(context.Route), token)
            .ConfigureAwait(false);

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default) =>
        _pipeConnector.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionId, token);

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        _pipeConnector.ConnectOutAsync<TMessage, TResult>(pipe, routingKey, token);

    public ValueTask DisposeAsync() => _pipeConnector.DisposeAsync();
}