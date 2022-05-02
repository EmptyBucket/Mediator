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

namespace ConsoleApp5.Registries;

internal class TopologyRegistry : ITopologyRegistry, IHandlerProvider, IPipeProvider
{
    private readonly ServiceFactory _serviceFactory;
    private readonly ITransportProvider _transportProvider;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<(Type, string), HashSet<IHandler>> _handlers = new();
    private readonly Dictionary<(Type, string), HashSet<Type>> _handlerTypes = new();
    private readonly Dictionary<(Type, string), HashSet<IPipe>> _pipes = new();
    private readonly Dictionary<(Type, string), HashSet<string>> _pipeNames = new();

    public TopologyRegistry(ServiceFactory serviceFactory, ITransportProvider transportProvider)
    {
        _serviceFactory = serviceFactory;
        _transportProvider = transportProvider;
    }

    public void AddTopology<TMessage>(IHandler handler, string transportName = "default", string routingKey = "")
    {
        var key = (typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        if (_handlers.TryAdd(key, new HashSet<IHandler>()))
        {
            _pipeNames.TryAdd(key, new HashSet<string>());
            _pipeNames[key].Add(transportName);
        }

        _handlers[key].Add(handler);

        _lock.ExitWriteLock();
    }

    public void AddTopology<TMessage>(IHandler handler, IPipe pipe, string routingKey = "")
    {
        var key = (typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        if (_handlers.TryAdd(key, new HashSet<IHandler>()))
        {
            _pipes.TryAdd(key, new HashSet<IPipe>());
            _pipes[key].Add(pipe);
        }

        _handlers[key].Add(handler);

        _lock.ExitWriteLock();
    }

    public void AddTopology<TMessage, THandler>(IPipe pipe, string routingKey = "") where THandler : IHandler
    {
        var key = (typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        if (_handlerTypes.TryAdd(key, new HashSet<Type>()))
        {
            _pipes.TryAdd(key, new HashSet<IPipe>());
            _pipes[key].Add(pipe);
        }

        _handlerTypes[key].Add(typeof(THandler));

        _lock.ExitWriteLock();
    }

    public void AddTopology<TMessage, THandler>(string transportName = "default", string routingKey = "")
        where THandler : IHandler
    {
        var key = (typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        if (_handlerTypes.TryAdd(key, new HashSet<Type>()))
        {
            _pipeNames.TryAdd(key, new HashSet<string>());
            _pipeNames[key].Add(transportName);
        }

        _handlerTypes[key].Add(typeof(THandler));

        _lock.ExitWriteLock();
    }

    public void RemoveTopology<TMessage>(IHandler handler, string routingKey = "")
    {
        var key = (typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        if (_handlers.TryGetValue(key, out var list))
        {
            list.Remove(handler);

            if (!list.Any())
            {
                _handlers.Remove(key);
                _pipes.Remove(key);
                _pipeNames.Remove(key);
            }
        }

        _lock.ExitWriteLock();
    }

    public void RemoveTopology<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler
    {
        var key = (typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        if (_handlerTypes.TryGetValue(key, out var list))
        {
            list.Remove(typeof(THandler));

            if (!list.Any())
            {
                _handlers.Remove(key);
                _pipes.Remove(key);
                _pipeNames.Remove(key);
            }
        }

        _lock.ExitWriteLock();
    }

    public IReadOnlyCollection<IHandler> GetHandlers<TMessage>(string routingKey = "")
    {
        var key = (typeof(TMessage), routingKey);

        _lock.EnterReadLock();

        if (_handlers.TryGetValue(key, out var handlers))
            return handlers.ToArray();

        if (_handlerTypes.TryGetValue(key, out var handlerTypes))
            return handlerTypes.Select(h => _serviceFactory(h)).Cast<IHandler>().ToArray();

        _lock.ExitReadLock();

        return Array.Empty<IHandler>();
    }

    public IReadOnlyCollection<IPipe> GetPipes<TMessage>(string routingKey = "")
    {
        var key = (typeof(TMessage), routingKey);

        _lock.EnterReadLock();

        if (_pipeNames.TryGetValue(key, out var pipeNames))
            return pipeNames.Select(t => _transportProvider.GetTransport(t)).ToArray();

        _lock.ExitReadLock();

        return Array.Empty<IPipe>();
    }
}