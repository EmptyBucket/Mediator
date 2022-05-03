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

using ConsoleApp5.Pipes;
using EasyNetQ;

namespace ConsoleApp5.Bindings;

internal class RabbitMqPipeBinder : IPipeBinder
{
    private readonly IBus _bus;
    private readonly Dictionary<Route, IDisposable> _subscriptions = new();

    public RabbitMqPipeBinder(IBus bus)
    {
        _bus = bus;
    }

    public async Task Bind<TMessage>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), RoutingKey: routingKey);

        if (!_subscriptions.ContainsKey(route))
        {
            var subscription = await _bus.PubSub.SubscribeAsync<TMessage>(routingKey,
                (m, c) => pipe.Handle(m, new MessageOptions(routingKey), c),
                c => c.WithDurable(false).WithAutoDelete().WithTopic(route.ToString()));
            _subscriptions[route] = subscription;
        }
    }

    public async Task Bind<TMessage, TResult>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), ResultType: typeof(TResult), RoutingKey: routingKey);

        if (!_subscriptions.ContainsKey(route))
        {
            var subscription = await _bus.Rpc.RespondAsync<TMessage, TResult>(
                (m, c) => pipe.Handle<TMessage, TResult>(m, new MessageOptions(routingKey), c),
                c => c.WithDurable(false).WithQueueName(route.ToString()));
            _subscriptions[route] = subscription;
        }
    }

    public Task Unbind<TMessage>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), RoutingKey: routingKey);

        if (_subscriptions.Remove(route, out var topology)) topology.Dispose();

        return Task.CompletedTask;
    }

    public Task Unbind<TMessage, TResult>(IPipe pipe, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), ResultType: typeof(TResult), RoutingKey: routingKey);

        if (_subscriptions.Remove(route, out var topology)) topology.Dispose();

        return Task.CompletedTask;
    }
}