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

using FlexMediator.Handlers;
using FlexMediator.Pipes;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediator.Topologies;

public class DirectTopologyBinder : ITopologyBinder
{
    private readonly HandlingPipe _receivePipe;
    private readonly BranchingPipe _dispatchPipe;

    public DirectTopologyBinder(BranchingPipe dispatchPipe, IServiceScopeFactory serviceScopeFactory)
    {
        _dispatchPipe = dispatchPipe;
        _receivePipe = new HandlingPipe(serviceScopeFactory);
    }

    public async Task<TopologyBind> BindDispatch<TMessage>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);
        var pipeBind = await _dispatchPipe.Bind<TMessage>(_receivePipe, routingKey);
        return new TopologyBind(Unbind, route, pipeBind);
    }

    public async Task<TopologyBind> BindDispatch<TMessage, TResult>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey, typeof(TResult));
        var pipeBind = await _dispatchPipe.Bind<TMessage, TResult>(_receivePipe, routingKey);
        return new TopologyBind(Unbind, route, pipeBind);
    }

    public async Task<TopologyBind> BindReceive<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var route = new Route(typeof(TMessage), routingKey);
        var handlerBind = _receivePipe.Bind<TMessage, THandler>(routingKey);
        return new TopologyBind(Unbind, route, pipeBind, handlerBind);
    }

    public async Task<TopologyBind> BindReceive<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>
    {
        var route = new Route(typeof(TMessage), routingKey, typeof(TResult));
        var pipeBind = await _dispatchPipe.Bind<TMessage, TResult>(_receivePipe);
        var handlerBind = _receivePipe.Bind<TMessage, TResult, THandler>(routingKey);
        return new TopologyBind(Unbind, route, pipeBind, handlerBind);
    }

    public async Task<TopologyBind> BindReceive<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);
        var pipeBind = await _dispatchPipe.Bind<TMessage>(_receivePipe);
        var handlerBind = _receivePipe.Bind(handler, routingKey);
        return new TopologyBind(Unbind, route, pipeBind, handlerBind);
    }

    public async Task<TopologyBind> BindReceive<TMessage, TResult>(IHandler<TMessage, TResult> handler,
        string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey, typeof(TResult));
        var pipeBind = await _dispatchPipe.Bind<TMessage, TResult>(_receivePipe);
        var handlerBind = _receivePipe.Bind(handler, routingKey);
        return new TopologyBind(Unbind, route, pipeBind, handlerBind);
    }

    private static async ValueTask Unbind(TopologyBind topologyBind)
    {
        topologyBind.HandlerBind?.Dispose();
        await topologyBind.PipeBind.DisposeAsync();
    }
}