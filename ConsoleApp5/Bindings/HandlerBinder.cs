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

internal class HandlerBinder : IHandlerBinder, IHandlerBindProvider
{
    private readonly Dictionary<Route, HashSet<HandlerBind>> _handlerBindings = new();

    public void Bind<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), RoutingKey: routingKey);

        _handlerBindings.TryAdd(route, new HashSet<HandlerBind>());
        _handlerBindings[route].Add(new HandlerBind(route, handler: handler));
    }

    public void Bind<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), ResultType: typeof(TResult), RoutingKey: routingKey);

        _handlerBindings.TryAdd(route, new HashSet<HandlerBind>());
        _handlerBindings[route].Add(new HandlerBind(route, handler: handler));
    }

    public void Bind<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var route = new Route(typeof(TMessage), RoutingKey: routingKey);

        _handlerBindings.TryAdd(route, new HashSet<HandlerBind>());
        _handlerBindings[route].Add(new HandlerBind(route, handlerType: typeof(THandler)));
    }

    public void Bind<TMessage, TResult, THandler>(string routingKey = "") 
        where THandler : IHandler<TMessage, TResult>
    {
        var route = new Route(typeof(TMessage), ResultType: typeof(TResult), RoutingKey: routingKey);

        _handlerBindings.TryAdd(route, new HashSet<HandlerBind>());
        _handlerBindings[route].Add(new HandlerBind(route, handlerType: typeof(THandler)));
    }

    public void Unbind<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), RoutingKey: routingKey);

        if (_handlerBindings.TryGetValue(route, out var set))
        {
            set.Remove(new HandlerBind(route, handler: handler));

            if (!set.Any()) _handlerBindings.Remove(route);
        }
    }

    public void Unbind<TMessage, TResult>(IHandler<TMessage, TResult> handler, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), ResultType: typeof(TResult), RoutingKey: routingKey);

        if (_handlerBindings.TryGetValue(route, out var set))
        {
            set.Remove(new HandlerBind(route, handler: handler));

            if (!set.Any()) _handlerBindings.Remove(route);
        }
    }

    public void Unbind<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var route = new Route(typeof(TMessage), RoutingKey: routingKey);

        if (_handlerBindings.TryGetValue(route, out var set))
        {
            set.Remove(new HandlerBind(route, handlerType: typeof(THandler)));

            if (!set.Any()) _handlerBindings.Remove(route);
        }
    }

    public void Unbind<TMessage, TResult, THandler>(string routingKey = "") 
        where THandler : IHandler<TMessage, TResult>
    {
        var route = new Route(typeof(TMessage), ResultType: typeof(TResult), RoutingKey: routingKey);

        if (_handlerBindings.TryGetValue(route, out var set))
        {
            set.Remove(new HandlerBind(route, handlerType: typeof(THandler)));

            if (!set.Any()) _handlerBindings.Remove(route);
        }
    }

    public IEnumerable<HandlerBind> GetBindings<TMessage>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), RoutingKey: routingKey);

        return _handlerBindings.TryGetValue(route, out var bindings) ? bindings : Enumerable.Empty<HandlerBind>();
    }
    
    public IEnumerable<HandlerBind> GetBindings<TMessage, TResult>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), ResultType: typeof(TResult), RoutingKey: routingKey);

        return _handlerBindings.TryGetValue(route, out var bindings) ? bindings : Enumerable.Empty<HandlerBind>();
    }
}