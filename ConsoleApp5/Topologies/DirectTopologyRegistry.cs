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

using System.Collections.Concurrent;
using ConsoleApp5.Models;
using ConsoleApp5.Pipes;

namespace ConsoleApp5.Topologies;

internal class DirectTopologyRegistry : ITopologyRegistry, ITopologyProvider
{
    private readonly IPipe _pipe;
    private readonly ConcurrentDictionary<Route, Topology> _topologies = new();

    public DirectTopologyRegistry(IPipe pipe)
    {
        _pipe = pipe;
    }

    public Task AddTopology<TMessage>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _topologies[route] = new Topology(route, _pipe);

        return Task.CompletedTask;
    }

    public Task AddTopology<TMessage, TResult>(string routingKey = "")
    {
        return AddTopology<TMessage>(routingKey);
    }

    public Task RemoveTopology<TMessage>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _topologies.Remove(route, out _);

        return Task.CompletedTask;
    }

    public Topology? GetTopology<TMessage>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        return _topologies.TryGetValue(route, out var topology) ? topology : null;
    }
}