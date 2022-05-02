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

namespace ConsoleApp5.Registries;

internal class MemoryHandlerRegistry : IHandlerRegistry
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<RegistryKey, HashSet<RegistryEntry>> _handlers = new();

    public Task AddHandler<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var key = new RegistryKey(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _handlers.TryAdd(key, new HashSet<RegistryEntry>());
            _handlers[key].Add(new RegistryEntry(Handler: handler));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task AddHandler<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "")
    {
        var key = new RegistryKey(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _handlers.TryAdd(key, new HashSet<RegistryEntry>());
            _handlers[key].Add(new RegistryEntry(Handler: handler));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task AddHandler<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var key = new RegistryKey(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _handlers.TryAdd(key, new HashSet<RegistryEntry>());
            _handlers[key].Add(new RegistryEntry(HandlerType: typeof(THandler)));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task AddHandler<TMessage, THandler, TResult>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>
    {
        var key = new RegistryKey(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _handlers.TryAdd(key, new HashSet<RegistryEntry>());
            _handlers[key].Add(new RegistryEntry(HandlerType: typeof(THandler)));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task RemoveHandler<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var key = new RegistryKey(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (_handlers.TryGetValue(key, out var set))
            {
                set.Remove(new RegistryEntry(Handler: handler));

                if (!set.Any()) _handlers.Remove(key);
            }
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
        var key = new RegistryKey(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (_handlers.TryGetValue(key, out var set))
            {
                set.Remove(new RegistryEntry(HandlerType: typeof(THandler)));

                if (!set.Any()) _handlers.Remove(key);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    private record RegistryKey(Type MessageType, string RoutingKey = "");

    private record RegistryEntry(IHandler? Handler = null, Type? HandlerType = null);
}