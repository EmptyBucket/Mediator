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

public class RabbitMqTopologyFactory : ITopologyFactory
{
    private readonly IBus _bus;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public RabbitMqTopologyFactory(IBus bus, IServiceScopeFactory serviceScopeFactory)
    {
        _bus = bus;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public Topology Create(IPipeBinder dispatchPipeBinder)
    {
        var handlerBinder = new HandlerBinder();
        var receivePipeBinder = new RabbitMqPipeBinder(_bus);
        var dispatchPipe = new RabbitMqPipe(_bus);
        var receivePipe = new HandlerPipe(handlerBinder, _serviceScopeFactory);
        return new Topology(dispatchPipe, receivePipe, _dispatchPipeBinder, receivePipeBinder, handlerBinder);
    }
}