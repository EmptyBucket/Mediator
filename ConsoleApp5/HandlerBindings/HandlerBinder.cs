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

using ConsoleApp5.Models;

namespace ConsoleApp5.HandlerBindings;

internal class HandlerBinder : IHandlerBinder, IHandlerBindProvider
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<Route, HashSet<HandlerBind>> _handlerBindings = new();

    public Task Bind<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _handlerBindings.TryAdd(route, new HashSet<HandlerBind>());
            _handlerBindings[route].Add(new HandlerBind(route, handler: handler));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task Bind<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _handlerBindings.TryAdd(route, new HashSet<HandlerBind>());
            _handlerBindings[route].Add(new HandlerBind(route, handler: handler));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task Bind<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _handlerBindings.TryAdd(route, new HashSet<HandlerBind>());
            _handlerBindings[route].Add(new HandlerBind(route, handlerType: typeof(THandler)));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task Bind<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _handlerBindings.TryAdd(route, new HashSet<HandlerBind>());
            _handlerBindings[route].Add(new HandlerBind(route, handlerType: typeof(THandler)));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task Unbind<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (_handlerBindings.TryGetValue(route, out var set))
            {
                set.Remove(new HandlerBind(route, handler: handler));

                if (!set.Any()) _handlerBindings.Remove(route);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task Unbind<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (_handlerBindings.TryGetValue(route, out var set))
            {
                set.Remove(new HandlerBind(route, handlerType: typeof(THandler)));

                if (!set.Any()) _handlerBindings.Remove(route);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public IEnumerable<HandlerBind> GetBindings<TMessage>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        return _handlerBindings.TryGetValue(route, out var bindings) ? bindings : Enumerable.Empty<HandlerBind>();
    }
}