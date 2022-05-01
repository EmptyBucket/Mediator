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

using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp5.Registries;

internal class HandlerRegistry : IHandlerRegistry, IHandlerProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<(Type, string?), List<IHandler>> _handlers = new();
    private readonly Dictionary<(Type, string?), List<Type>> _handlerTypes = new();

    public HandlerRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void AddHandler<TMessage>(IHandler handler, string? routingKey = null)
    {
        var key = (typeof(TMessage), routingKey);
        _lock.EnterWriteLock();
        _handlers.TryAdd(key, new List<IHandler>());
        _handlers[key].Add(handler);
        _lock.ExitWriteLock();
    }

    public void AddHandler<TMessage, THandler>(string? routingKey = null) where THandler : IHandler
    {
        var key = (typeof(TMessage), routingKey);
        _lock.EnterWriteLock();
        _handlerTypes.TryAdd(key, new List<Type>());
        _handlerTypes[key].Add(typeof(THandler));
        _lock.ExitWriteLock();
    }

    public void RemoveHandler<TMessage>(IHandler handler, string? routingKey = null)
    {
        var key = (typeof(TMessage), routingKey);
        _lock.EnterWriteLock();

        if (_handlers.TryGetValue(key, out var list))
        {
            list.Remove(handler);

            if (!list.Any()) _handlers.Remove(key);
        }

        _lock.ExitWriteLock();
    }

    public void RemoveHandler<TMessage, THandler>(string? routingKey = null) where THandler : IHandler
    {
        var key = (typeof(TMessage), routingKey);
        _lock.EnterWriteLock();

        if (_handlerTypes.TryGetValue(key, out var list))
        {
            list.Remove(typeof(THandler));

            if (!list.Any()) _handlers.Remove(key);
        }

        _lock.ExitWriteLock();
    }

    public IReadOnlyCollection<IHandler> GetHandlers<TMessage>(string? routingKey = null)
    {
        var key = (typeof(TMessage), routingKey);

        _lock.EnterReadLock();
        if (_handlers.TryGetValue(key, out var handlers))
            return handlers.ToArray();

        if (_handlerTypes.TryGetValue(key, out var handlerTypes))
            return handlerTypes.Select(t => _serviceProvider.GetRequiredService(t)).Cast<IHandler>().ToArray();
        _lock.ExitReadLock();

        return Array.Empty<IHandler>();
    }
}