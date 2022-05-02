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
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp5.Registries;

internal class RabbitMqHandlerRegistry : IHandlerRegistry
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IBus _bus;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<RegistryKey, IDisposable> _subscriptions = new();

    public RabbitMqHandlerRegistry(IServiceScopeFactory serviceScopeFactory, IBus bus)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _bus = bus;
    }

    public async Task AddHandler<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var key = new RegistryKey(typeof(TMessage), routingKey, Handler: handler);

        _lock.EnterWriteLock();

        try
        {
            if (!_subscriptions.ContainsKey(key))
            {
                var subscription = await _bus.PubSub.SubscribeAsync<TMessage>(routingKey,
                    (m, c) => handler.Handle(m!, new MessageOptions(routingKey), c),
                    c => c.WithDurable(false).WithAutoDelete());
                _subscriptions[key] = subscription;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public async Task AddHandler<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "")
    {
        var key = new RegistryKey(typeof(TMessage), routingKey, Handler: handler);

        _lock.EnterWriteLock();

        try
        {
            if (!_subscriptions.ContainsKey(key))
            {
                var subscription = await _bus.Rpc.RespondAsync<TMessage, TResult>(
                    (m, c) => handler.Handle(m!, new MessageOptions(routingKey), c),
                    c => c.WithDurable(false));
                _subscriptions[key] = subscription;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public async Task AddHandler<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var key = new RegistryKey(typeof(TMessage), routingKey, HandlerType: typeof(THandler));

        _lock.EnterWriteLock();

        try
        {
            if (!_subscriptions.ContainsKey(key))
            {
                var subscription = await _bus.PubSub.SubscribeAsync<TMessage>(routingKey, async (m, c) =>
                    {
                        using var serviceScope = _serviceScopeFactory.CreateScope();
                        var handler = serviceScope.ServiceProvider.GetRequiredService<THandler>();
                        await handler.Handle(m!, new MessageOptions(routingKey), c);
                    },
                    c => c.WithDurable(false).WithAutoDelete());
                _subscriptions[key] = subscription;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public async Task AddHandler<TMessage, THandler, TResult>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>
    {
        var key = new RegistryKey(typeof(TMessage), routingKey, HandlerType: typeof(THandler));

        _lock.EnterWriteLock();

        try
        {
            if (!_subscriptions.ContainsKey(key))
            {
                var subscription = await _bus.Rpc.RespondAsync<TMessage, TResult>(async (m, c) =>
                    {
                        using var serviceScope = _serviceScopeFactory.CreateScope();
                        var handler = serviceScope.ServiceProvider.GetRequiredService<THandler>();
                        return await handler.Handle(m!, new MessageOptions(routingKey), c);
                    },
                    c => c.WithDurable(false));
                _subscriptions[key] = subscription;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task RemoveHandler<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var key = new RegistryKey(typeof(TMessage), routingKey, Handler: handler);

        _lock.EnterWriteLock();

        try
        {
            if (_subscriptions.Remove(key, out var subscription)) subscription.Dispose();
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task RemoveHandler<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var key = new RegistryKey(typeof(TMessage), routingKey, HandlerType: typeof(THandler));

        _lock.EnterWriteLock();

        try
        {
            if (_subscriptions.Remove(key, out var subscription)) subscription.Dispose();
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    private record RegistryKey(Type MessageType, string RoutingKey, IHandler? Handler = null, Type? HandlerType = null);
}