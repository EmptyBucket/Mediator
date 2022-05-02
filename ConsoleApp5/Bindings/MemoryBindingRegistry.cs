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

namespace ConsoleApp5.Bindings;

internal class MemoryBindingRegistry : IBindingRegistry, IBindingProvider
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<Route, HashSet<Binding>> _bindings = new();

    public Task Add<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var key = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _bindings.TryAdd(key, new HashSet<Binding>());
            _bindings[key].Add(new Binding<TMessage>(routingKey, handler: handler));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task Add<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "")
    {
        var key = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _bindings.TryAdd(key, new HashSet<Binding>());
            _bindings[key].Add(new Binding<TMessage>(routingKey, handler: handler));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task Add<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var key = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _bindings.TryAdd(key, new HashSet<Binding>());
            _bindings[key].Add(new Binding<TMessage>(routingKey, handlerType: typeof(THandler)));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task Add<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>
    {
        var key = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _bindings.TryAdd(key, new HashSet<Binding>());
            _bindings[key].Add(new Binding<TMessage>(routingKey, handlerType: typeof(THandler)));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task Remove<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var key = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (_bindings.TryGetValue(key, out var set))
            {
                set.Remove(new Binding<TMessage>(routingKey, handler: handler));

                if (!set.Any()) _bindings.Remove(key);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task Remove<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var key = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (_bindings.TryGetValue(key, out var set))
            {
                set.Remove(new Binding<TMessage>(routingKey, handlerType: typeof(THandler)));

                if (!set.Any()) _bindings.Remove(key);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public IEnumerable<Binding<TMessage>> Get<TMessage>(string routingKey = "")
    {
        var key = new Route(typeof(TMessage), routingKey);
        
        return _bindings[key].Cast<Binding<TMessage>>();
    }

    private record Route(Type MessageType, string RoutingKey = "");
}