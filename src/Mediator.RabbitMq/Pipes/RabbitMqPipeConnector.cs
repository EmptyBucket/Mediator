using System.Collections.Concurrent;
using EasyNetQ;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.PublishSubscribe;
using Mediator.Pipes.RequestResponse;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.RabbitMq.Pipes;

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

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default)
    {
        async Task Handle(TMessage m, CancellationToken c)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var messageContext = new MessageContext(scope.ServiceProvider, routingKey);
            await pipe.PassAsync(m, messageContext, c);
        }

        var route = Route.For<TMessage>(routingKey);
        var subscription = await _bus.PubSub
            .SubscribeAsync<TMessage>(subscriptionId, Handle, c => c.WithTopic(route), token)
            .ConfigureAwait(false);
        var pipeConnection = new PipeConnection(subscription, p => _pipeConnections.TryRemove(p, out _));
        _pipeConnections.TryAdd(pipeConnection, pipeConnection);
        return pipeConnection;
    }

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        async Task<TResult> Handle(TMessage m, CancellationToken c)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var messageContext = new MessageContext(scope.ServiceProvider, routingKey);
            return await pipe.PassAsync<TMessage, TResult>(m, messageContext, c);
        }

        var route = Route.For<TMessage, TResult>(routingKey);
        var subscription = await _bus.Rpc
            .RespondAsync<TMessage, TResult>(Handle, c => c.WithQueueName(route), token)
            .ConfigureAwait(false);
        var pipeConnection = new PipeConnection(subscription, p => _pipeConnections.TryRemove(p, out _));
        _pipeConnections.TryAdd(pipeConnection, pipeConnection);
        return pipeConnection;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Values) await pipeConnection.DisposeAsync();
    }

    private class PipeConnection : IAsyncDisposable
    {
        private readonly IDisposable _subscription;
        private readonly Action<PipeConnection> _callback;

        public PipeConnection(IDisposable subscription, Action<PipeConnection> callback)
        {
            _subscription = subscription;
            _callback = callback;
        }

        public ValueTask DisposeAsync()
        {
            _subscription.Dispose();
            _callback(this);
            return ValueTask.CompletedTask;
        }
    }
}