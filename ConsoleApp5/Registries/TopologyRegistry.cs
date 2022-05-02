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

internal class TopologyRegistry : ITopologyRegistry, ITopologyProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<(Type, string), HashSet<RegistryEntry>> _topologies = new();

    public TopologyRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void AddTopology<TMessage>(IHandler handler, string pipeName = "default", string routingKey = "")
    {
        var key = (typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _topologies.TryAdd(key, new HashSet<RegistryEntry>());
            _topologies[key].Add(new RegistryEntry(pipeName, Handler: handler));
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void AddTopology<TMessage, THandler>(string pipeName = "default", string routingKey = "")
        where THandler : IHandler
    {
        var key = (typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _topologies.TryAdd(key, new HashSet<RegistryEntry>());
            _topologies[key].Add(new RegistryEntry(pipeName, HandlerType: typeof(THandler)));
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void RemoveTopology<TMessage>(IHandler handler, string pipeName = "default", string routingKey = "")
    {
        var key = (typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (_topologies.TryGetValue(key, out var set))
            {
                set.Remove(new RegistryEntry(pipeName, Handler: handler));

                if (!set.Any()) _topologies.Remove(key);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void RemoveTopology<TMessage, THandler>(string pipeName = "default", string routingKey = "")
        where THandler : IHandler
    {
        var key = (typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (_topologies.TryGetValue(key, out var set))
            {
                set.Remove(new RegistryEntry(pipeName, HandlerType: typeof(THandler)));

                if (!set.Any()) _topologies.Remove(key);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public IEnumerable<(string PipeName, IHandler Handler)> GetTopologies<TMessage>(string routingKey = "")
    {
        var key = (typeof(TMessage), routingKey);

        _lock.EnterReadLock();

        try
        {
            if (_topologies.TryGetValue(key, out var topologies))
                return topologies
                    .Select(t =>
                        (t.PipeName, t.Handler ?? (IHandler)_serviceProvider.GetRequiredService(t.HandlerType!)))
                    .ToArray();
        }
        finally
        {
            _lock.ExitReadLock();
        }

        return Array.Empty<(string PipeName, IHandler Handler)>();
    }

    private record RegistryEntry(string PipeName, IHandler? Handler = null, Type? HandlerType = null);
}