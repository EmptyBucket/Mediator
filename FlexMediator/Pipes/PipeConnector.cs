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

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using FlexMediator.Utils;

namespace FlexMediator.Pipes;

public class PipeConnector : IPipeConnector, IPipeConnections
{
    private readonly Dictionary<Route, HashSet<PipeConnection>> _pipeConnections = new();

    public Task<PipeConnection> Into<TMessage>(IPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        Connect(Route.For<TMessage>(routingKey), pipe);

    public Task<PipeConnection> Into<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        Connect(Route.For<TMessage, TResult>(routingKey), pipe);

    private Task<PipeConnection> Connect(Route route, IPipe pipe)
    {
        var pipeConnection = new PipeConnection(route, pipe, Disconnect);

        _pipeConnections.TryAdd(route, new HashSet<PipeConnection>());
        _pipeConnections[route].Add(pipeConnection);

        return Task.FromResult(pipeConnection);
    }

    private ValueTask Disconnect(PipeConnection pipeConnection)
    {
        if (_pipeConnections.TryGetValue(pipeConnection.Route, out var set))
        {
            set.Remove(pipeConnection);

            if (!set.Any()) _pipeConnections.Remove(pipeConnection.Route);
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Values.SelectMany(c => c)) await pipeConnection.DisposeAsync();
    }

    #region IReadOnlyDictionary implementation

    public IEnumerator<KeyValuePair<Route, IReadOnlySet<PipeConnection>>> GetEnumerator() =>
        _pipeConnections.Cast<KeyValuePair<Route, IReadOnlySet<PipeConnection>>>().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _pipeConnections.Count;

    public bool ContainsKey(Route key) => _pipeConnections.ContainsKey(key);

    public bool TryGetValue(Route key, [MaybeNullWhen(false)] out IReadOnlySet<PipeConnection> value)
    {
        var tryGetValue = _pipeConnections.TryGetValue(key, out var set);
        value = set;
        return tryGetValue;
    }

    public IReadOnlySet<PipeConnection> this[Route key] => _pipeConnections[key];

    public IEnumerable<Route> Keys => _pipeConnections.Keys;

    public IEnumerable<IReadOnlySet<PipeConnection>> Values => _pipeConnections.Values;

    #endregion
}