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

using EasyNetQ;
using FlexMediator.Handlers;
using FlexMediator.Pipes;
using FlexMediator.Topologies;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediatorRabbit;

public class RabbitMqTopologyBinder : ITopologyBinder
{
    private readonly RabbitMqPipe _boundDispatchPipe;
    private readonly HandlingPipe _boundReceivePipe;
    private readonly BranchingPipe _dispatchPipe;
    private readonly RabbitMqPipeBinder _receivePipeBinder;

    public RabbitMqTopologyBinder(BranchingPipe dispatchPipe, IBus bus, IServiceScopeFactory serviceScopeFactory)
    {
        _dispatchPipe = dispatchPipe;
        _receivePipeBinder = new RabbitMqPipeBinder(bus);
        
        _boundDispatchPipe = new RabbitMqPipe(bus);
        _boundReceivePipe = new HandlingPipe(serviceScopeFactory);
    }

    public async Task<TopologyBind> BindDispatch<TMessage>(string routingKey = "")
    {
        var pipeBind = await _dispatchPipe.Bind<TMessage>(_boundDispatchPipe, routingKey);
    }

    public async Task<TopologyBind> BindDispatch<TMessage, TResult>(string routingKey = "")
    {
        var pipeBind = await _dispatchPipe.Bind<TMessage, TResult>(_boundDispatchPipe, routingKey);
    }

    public async Task<TopologyBind> BindReceive<TMessage, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage>
    {
        var pipeBind = await _receivePipeBinder.Bind<TMessage>(_boundReceivePipe);
        var handlerBind = _boundReceivePipe.Bind<TMessage, THandler>(routingKey);
    }

    public async Task<TopologyBind> BindReceive<TMessage, TResult, THandler>(string routingKey = "")
        where THandler : IHandler<TMessage, TResult>
    {
        var pipeBind = await _receivePipeBinder.Bind<TMessage, TResult>(_boundReceivePipe);
        var handlerBind = _boundReceivePipe.Bind<TMessage, TResult, THandler>(routingKey);
    }

    public async Task<TopologyBind> BindReceive<TMessage>(IHandler<TMessage> handler, string routingKey = "")
    {
        var pipeBind = await _receivePipeBinder.Bind<TMessage>(_boundReceivePipe);
        var handlerBind = _boundReceivePipe.Bind(handler, routingKey);
    }

    public async Task<TopologyBind> BindReceive<TMessage, TResult>(IHandler<TMessage, TResult> handler,
        string routingKey = "")
    {
        var pipeBind = await _receivePipeBinder.Bind<TMessage, TResult>(_boundReceivePipe);
        var handlerBind = _boundReceivePipe.Bind(handler, routingKey);
    }
}