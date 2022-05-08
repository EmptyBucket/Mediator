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

using FlexMediator.Utils;

namespace FlexMediator.Pipes;

public class Pipe : IPipe, IPipeConnector
{
    private readonly PipeConnector _pipeConnector;

    public Pipe()
    {
        _pipeConnector = new PipeConnector();
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options,
        CancellationToken token = default)
    {
        var route = new Route(typeof(TMessage), options.RoutingKey);
        var connections = _pipeConnector.GetValueOrDefault(route) ?? Enumerable.Empty<PipeConnection>();
        var pipes = connections.Select(c => c.Pipe);
        await Task.WhenAll(pipes.Select(p => p.Handle(message, options, token)));
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token = default)
    {
        var route = new Route(typeof(TMessage), options.RoutingKey, typeof(TResult));
        var connections = _pipeConnector.GetValueOrDefault(route) ?? Enumerable.Empty<PipeConnection>();
        var pipes = connections.Select(c => c.Pipe);
        return await pipes.Single().Handle<TMessage, TResult>(message, options, token);
    }

    public Task<PipeConnection> Out<TMessage>(IPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        _pipeConnector.Out<TMessage>(pipe, routingKey);

    public Task<PipeConnection> Out<TMessage, TResult>(IPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        _pipeConnector.Out<TMessage, TResult>(pipe, routingKey);
}