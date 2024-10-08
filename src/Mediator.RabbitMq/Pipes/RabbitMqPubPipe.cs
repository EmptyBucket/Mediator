// MIT License
// 
// Copyright (c) 2022 Alexey Politov
// https://github.com/EmptyBucket/Mediator
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Collections.Concurrent;
using EasyNetQ;
using EasyNetQ.Topology;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.Utils;
using Mediator.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.RabbitMq.Pipes;

internal class RabbitMqPubPipe : IMulticastPubPipe
{
    private int _isDisposed;
    private readonly IBus _bus;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<PipeConnection<IPubPipe>, IDisposable> _pipeConnections = new();

    public RabbitMqPubPipe(IBus bus, IServiceProvider serviceProvider)
    {
        _bus = bus;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task PassAsync<TMessage>(MessageContext<TMessage> ctx, CancellationToken cancellationToken = default)
    {
        var exchange = await DeclareExchangeAsync(ctx.Route, cancellationToken);
        var messageProperties = new PropertyBinder<MessageProperties>().Bind(ctx.ServiceProps).Build();
        var message = new Message<MessageContext<TMessage>>(ctx, messageProperties);
        await _bus.Advanced.PublishAsync(exchange, ctx.Route, false, message, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IEnumerable<PipeConnection<IPubPipe>> GetPubConnections() => _pipeConnections.Keys;

    /// <inheritdoc />
    public PipeConnection<IPubPipe> ConnectOut<TMessage>(IPubPipe pipe, string routingKey = "",
        string connectionName = "", string subscriptionId = "")
    {
        var route = Route.For<TMessage>(routingKey);
        var queue = DeclareQueue(route, subscriptionId);
        var pipeConnection = ConnectPipe<TMessage>(connectionName, route, pipe, queue);
        return pipeConnection;
    }

    /// <inheritdoc />
    public async Task<PipeConnection<IPubPipe>> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string connectionName = "", string subscriptionId = "", CancellationToken cancellationToken = default)
    {
        var route = Route.For<TMessage>(routingKey);
        var queue = await DeclareQueueAsync(route, subscriptionId, cancellationToken);
        var pipeConnection = ConnectPipe<TMessage>(connectionName, route, pipe, queue);
        return pipeConnection;
    }

    private PipeConnection<IPubPipe> ConnectPipe<TMessage>(string connectionName, Route route, IPubPipe pipe,
        IQueue queue)
    {
        var subscription = _bus.Advanced.Consume<MessageContext<TMessage>>(queue, (m, _) => HandleAsync(pipe, m));
        var pipeConnection = new PipeConnection<IPubPipe>(connectionName, route, pipe, DisconnectPipe);
        _pipeConnections.TryAdd(pipeConnection, subscription);
        return pipeConnection;
    }

    private void DisconnectPipe(PipeConnection<IPubPipe> p)
    {
        if (_pipeConnections.TryRemove(p, out var s)) s.Dispose();
    }

    private async Task HandleAsync<TMessage>(IPubPipe pipe, IMessage<MessageContext<TMessage>> message)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var ctx = message.Body with { DeliveredAt = DateTime.Now, ServiceProvider = scope.ServiceProvider };
        await pipe.PassAsync(ctx);
    }

    private IExchange DeclareExchange(Route route)
    {
        var exchange = _bus.Advanced.ExchangeDeclare(route, ExchangeType.Direct);
        return exchange;
    }

    private async Task<IExchange> DeclareExchangeAsync(Route route, CancellationToken cancellationToken = default)
    {
        var exchange = await _bus.Advanced
            .ExchangeDeclareAsync(route, ExchangeType.Direct, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return exchange;
    }

    private IQueue DeclareQueue(Route route, string subscriptionId)
    {
        var exchange = DeclareExchange(route);
        var queue = _bus.Advanced.QueueDeclare($"{route}:{subscriptionId}");
        _bus.Advanced.Bind(exchange, queue, route);
        return queue;
    }

    private async Task<IQueue> DeclareQueueAsync(Route route, string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var exchange = await DeclareExchangeAsync(route, cancellationToken);
        var queue = await _bus.Advanced.QueueDeclareAsync($"{route}:{subscriptionId}", cancellationToken)
            .ConfigureAwait(false);
        await _bus.Advanced.BindAsync(exchange, queue, route, cancellationToken).ConfigureAwait(false);
        return queue;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) return;

        foreach (var pipeConnection in _pipeConnections.Keys) await pipeConnection.DisposeAsync();
    }
}