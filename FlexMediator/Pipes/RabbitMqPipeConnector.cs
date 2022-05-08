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

using EasyNetQ;
using FlexMediator.Utils;

namespace FlexMediator.Pipes;

public class RabbitMqPipeConnector : IPipeConnector
{
    private readonly IBus _bus;
    private readonly Dictionary<Route, PipeConnection> _pipeConnections = new();

    public RabbitMqPipeConnector(IBus bus)
    {
        _bus = bus;
    }

    public async Task<PipeConnection> Out<TMessage>(IPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = new Route(typeof(TMessage), routingKey);

        if (_pipeConnections.TryGetValue(route, out var pipeConnection)) return pipeConnection;

        var subscription = await _bus.PubSub.SubscribeAsync<TMessage>(string.Empty,
            (m, c) => pipe.Handle(m, new MessageOptions(routingKey), c),
            c => c.WithQueueName(route).WithTopic(route), token);
        return _pipeConnections[route] = new PipeConnection(p => Disconnect(p, subscription), route, pipe);
    }

    public async Task<PipeConnection> Out<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        var route = new Route(typeof(TMessage), routingKey, typeof(TResult));

        if (_pipeConnections.TryGetValue(route, out var pipeConnection)) return pipeConnection;

        var subscription = await _bus.Rpc.RespondAsync<TMessage, TResult>(
            (m, c) => pipe.Handle<TMessage, TResult>(m, new MessageOptions(routingKey), c),
            c => c.WithQueueName(route), token);
        return _pipeConnections[route] = new PipeConnection(p => Disconnect(p, subscription), route, pipe);
    }

    private ValueTask Disconnect(PipeConnection pipeConnection, IDisposable subscription)
    {
        if (_pipeConnections.Remove(pipeConnection.Route)) subscription.Dispose();

        return ValueTask.CompletedTask;
    }
}