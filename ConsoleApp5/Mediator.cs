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

using ConsoleApp5.Bindings;
using ConsoleApp5.Pipes;
using ConsoleApp5.Topologies;

namespace ConsoleApp5;

internal class Mediator : IMediator
{
    private readonly TopologyRegistry _topologyRegistry;
    private readonly TransportRegistry _transportRegistry;
    private readonly IPipe _pipe;

    public Mediator(TopologyRegistry topologyRegistry, TransportRegistry transportRegistry)
    {
        _topologyRegistry = topologyRegistry;
        _transportRegistry = transportRegistry;
        _pipe = new TopologyPipe(topologyRegistry);
    }

    public async Task Publish<TMessage>(TMessage message, Action<MessageOptions>? optionsBuilder = null,
        CancellationToken token = default)
    {
        var messageOptions = new MessageOptions();
        optionsBuilder?.Invoke(messageOptions);
        await _pipe.Handle(message, messageOptions, token);
    }

    public async Task<TResult> Send<TMessage, TResult>(TMessage message, Action<MessageOptions>? optionsBuilder = null,
        CancellationToken token = default)
    {
        var messageOptions = new MessageOptions();
        optionsBuilder?.Invoke(messageOptions);
        return await _pipe.Handle<TMessage, TResult>(message, messageOptions, token);
    }

    public void AddTopology<TMessage>(IHandler handler, string transportName = "default", string routingKey = "")
    {
        _topologyRegistry.AddTopology<TMessage>(handler, transportName, routingKey);
    }

    public void AddTopology<TMessage, THandler>(string transportName = "default", string routingKey = "")
        where THandler : IHandler
    {
        _topologyRegistry.AddTopology<TMessage, THandler>(transportName, routingKey);
    }

    public void RemoveTopology<TMessage>(IHandler handler, string transportName = "default",
        string routingKey = "")
    {
        _topologyRegistry.RemoveTopology<TMessage>(handler, transportName, routingKey);
    }

    public void RemoveTopology<TMessage, THandler>(string transportName = "default", string routingKey = "")
        where THandler : IHandler
    {
        _topologyRegistry.RemoveTopology<TMessage, THandler>(transportName, routingKey);
    }

    public void AddPipe(string name, IPipe pipe)
    {
        _transportRegistry.AddPipe(name, pipe);
    }

    public void AddPipe<TPipe>(string name) where TPipe : IPipe
    {
        _transportRegistry.AddPipe<TPipe>(name);
    }

    public IEnumerable<(string PipeName, IHandler Handler)> GetTopologies<TMessage>(string routingKey = "")
    {
        return _topologyRegistry.GetTopologies<TMessage>(routingKey);
    }

    public (IPipe, IBindingRegistry) GetTransport(string transportName)
    {
        return _transportRegistry.GetTransport(transportName);
    }
}