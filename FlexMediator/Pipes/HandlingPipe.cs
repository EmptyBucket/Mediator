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
using FlexMediator.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace FlexMediator.Pipes;

public class HandlingPipe : IPipe, IHandleConnector
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly HandleConnector _handleConnector;

    public HandlingPipe(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _handleConnector = new HandleConnector();
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options, CancellationToken token)
    {
        var route = new Route(typeof(TMessage), options.RoutingKey);
        var connections = _handleConnector.GetValueOrDefault(route) ?? Enumerable.Empty<HandlerConnection>();

        using var serviceScope = _serviceScopeFactory.CreateScope();
        var serviceProvider = serviceScope.ServiceProvider;
        var handlers = connections.Select(c => c.Factory(serviceProvider)).Cast<IHandler<TMessage>>();
        await Task.WhenAll(handlers.Select(h => h.HandleAsync(message, options, token)));
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token)
    {
        var route = new Route(typeof(TMessage), options.RoutingKey, typeof(TResult));
        var connections = _handleConnector.GetValueOrDefault(route) ?? Enumerable.Empty<HandlerConnection>();

        using var serviceScope = _serviceScopeFactory.CreateScope();
        var serviceProvider = serviceScope.ServiceProvider;
        var handlers = connections.Select(c => c.Factory(serviceProvider)).Cast<IHandler<TMessage, TResult>>();
        return await handlers.Single().HandleAsync(message, options, token);
    }

    public HandlerConnection Out<TMessage>(Func<IServiceProvider, IHandler> factory,
        string routingKey = "") =>
        _handleConnector.Out<TMessage>(factory, routingKey);

    public HandlerConnection Out<TMessage, TResult>(Func<IServiceProvider, IHandler> factory,
        string routingKey = "") =>
        _handleConnector.Out<TMessage, TResult>(factory, routingKey);
}