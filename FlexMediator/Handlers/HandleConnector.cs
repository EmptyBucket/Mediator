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

namespace FlexMediator.Handlers;

public class HandleConnector : IHandleConnector, IHandlerConnections
{
    private readonly Dictionary<Route, HashSet<HandlerConnection>> _handlerConnections = new();

    public HandlerConnection BindHandler<TMessage>(Func<IServiceProvider, IHandler> factory, string routingKey = "") =>
        Out(Route.For<TMessage>(routingKey), factory);

    public HandlerConnection BindHandler<TMessage, TResult>(Func<IServiceProvider, IHandler> factory,
        string routingKey = "") =>
        Out(Route.For<TMessage, TResult>(routingKey), factory);

    private HandlerConnection Out(Route route, Func<IServiceProvider, IHandler> factory)
    {
        var handlerBind = new HandlerConnection(route, factory, Disconnect);

        _handlerConnections.TryAdd(route, new HashSet<HandlerConnection>());
        _handlerConnections[route].Add(handlerBind);

        return handlerBind;
    }

    private void Disconnect(HandlerConnection handlerConnection)
    {
        if (_handlerConnections.TryGetValue(handlerConnection.Route, out var set))
        {
            set.Remove(handlerConnection);

            if (!set.Any()) _handlerConnections.Remove(handlerConnection.Route);
        }
    }

    #region IReadOnlyDictionary implementation

    public IEnumerator<KeyValuePair<Route, IReadOnlySet<HandlerConnection>>> GetEnumerator() =>
        _handlerConnections.Cast<KeyValuePair<Route, IReadOnlySet<HandlerConnection>>>().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _handlerConnections.Count;

    public bool ContainsKey(Route key) => _handlerConnections.ContainsKey(key);

    public bool TryGetValue(Route key, [MaybeNullWhen(false)] out IReadOnlySet<HandlerConnection> value)
    {
        var tryGetValue = _handlerConnections.TryGetValue(key, out var set);
        value = set;
        return tryGetValue;
    }

    public IReadOnlySet<HandlerConnection> this[Route key] => _handlerConnections[key];

    public IEnumerable<Route> Keys => _handlerConnections.Keys;

    public IEnumerable<IReadOnlySet<HandlerConnection>> Values => _handlerConnections.Values;

    #endregion
}