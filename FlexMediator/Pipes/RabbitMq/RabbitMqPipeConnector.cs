using System.Collections.Concurrent;
using EasyNetQ;
using FlexMediator.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediator.Pipes.RabbitMq;

public class RabbitMqPipeConnector : IPipeConnector
{
    private readonly IBus _bus;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<PipeConnection, PipeConnection> _pipeConnections = new();

    public RabbitMqPipeConnector(IBus bus, IServiceProvider serviceProvider)
    {
        _bus = bus;
        _serviceProvider = serviceProvider;
    }

    public async Task<PipeConnection> ConnectInAsync<TMessage>(IPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = Route.For<TMessage>(routingKey);
        //todo нужно сделать subscriptionId
        var subscription = await _bus.PubSub.SubscribeAsync<TMessage>(string.Empty, async (m, c) =>
                {
                    await using var scope = _serviceProvider.CreateAsyncScope();
                    var messageContext = new MessageContext(scope.ServiceProvider, routingKey);
                    await pipe.PassAsync(m, messageContext, c);
                },
                c => c.WithQueueName(route).WithTopic(route), token)
            .ConfigureAwait(false);
        var pipeConnection = new PipeConnection(route, pipe, p => Disconnect(p, subscription));
        Connect(pipeConnection);
        return pipeConnection;
    }

    public async Task<PipeConnection> ConnectInAsync<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>(routingKey);
        var subscription = await _bus.Rpc.RespondAsync<TMessage, TResult>(async (m, c) =>
                {
                    await using var scope = _serviceProvider.CreateAsyncScope();
                    var messageContext = new MessageContext(scope.ServiceProvider, routingKey);
                    return await pipe.PassAsync<TMessage, TResult>(m, messageContext, c);
                },
                c => c.WithQueueName(route), token)
            .ConfigureAwait(false);
        var pipeConnection = new PipeConnection(route, pipe, p => Disconnect(p, subscription));
        Connect(pipeConnection);
        return pipeConnection;
    }

    private void Connect(PipeConnection pipeConnection) => _pipeConnections.TryAdd(pipeConnection, pipeConnection);

    private ValueTask Disconnect(PipeConnection pipeConnection, IDisposable unsubscribe)
    {
        if (_pipeConnections.TryRemove(pipeConnection, out _)) unsubscribe.Dispose();

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Values) await pipeConnection.DisposeAsync();
    }
}