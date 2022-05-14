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
        var route = Route.For<TMessage>(routingKey);
        var subscription = await _bus.PubSub.SubscribeAsync<TMessage>(subscriptionId, async (m, c) =>
                {
                    await using var scope = _serviceProvider.CreateAsyncScope();
                    var messageContext = new MessageContext(scope.ServiceProvider, routingKey);
                    await pipe.PassAsync(m, messageContext, c);
                },
                c => c.WithTopic(route), token)
            .ConfigureAwait(false);
        var pipeConnection = new PipeConnection(p => Disconnect(p, subscription));
        Connect(pipeConnection);
        return pipeConnection;
    }

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
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
        var pipeConnection = new PipeConnection(p => Disconnect(p, subscription));
        Connect(pipeConnection);
        return pipeConnection;
    }

    private void Connect(PipeConnection pipeConnection) => _pipeConnections.TryAdd(pipeConnection, pipeConnection);

    private void Disconnect(PipeConnection pipeConnection, IDisposable subscription)
    {
        if (_pipeConnections.TryRemove(pipeConnection, out _)) subscription.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Values) await pipeConnection.DisposeAsync();
    }

    private class PipeConnection : IAsyncDisposable
    {
        private readonly Action<PipeConnection> _disconnect;

        public PipeConnection(Action<PipeConnection> disconnect)
        {
            _disconnect = disconnect;
        }

        public ValueTask DisposeAsync()
        {
            _disconnect(this);
            return ValueTask.CompletedTask;
        }
    }
}