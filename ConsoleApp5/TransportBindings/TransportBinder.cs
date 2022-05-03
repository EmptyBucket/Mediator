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

namespace ConsoleApp5.TransportBindings;

internal class TransportBinder : ITransportBinder, ITransportBindProvider
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<Route, HashSet<TransportBind>> _transportBindings = new();

    public Task Bind<TMessage>(Transport transport, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _transportBindings.TryAdd(route, new HashSet<TransportBind>());
            _transportBindings[route].Add(new TransportBind(route, transport));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task Unbind<TMessage>(Transport transport, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (_transportBindings.TryGetValue(route, out var set))
            {
                set.Remove(new TransportBind(route, transport));

                if (!set.Any()) _transportBindings.Remove(route);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public IEnumerable<TransportBind> GetBinds<TMessage>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        return _transportBindings.TryGetValue(route, out var transportBinds)
            ? transportBinds
            : Enumerable.Empty<TransportBind>();
    }
}