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

using System.Collections.Immutable;
using Mediator.Handlers;
using Mediator.Pipes.Utils;

namespace Mediator;

internal class Mediator : IMediator
{
    public Mediator(MediatorTopology topology)
    {
        Topology = topology;
    }

    public async Task PublishAsync<TMessage>(TMessage message, Options? options = null,
        CancellationToken token = default)
    {
        options ??= new Options();
        var route = Route.For<TMessage>(options.RoutingKey);
        var ctx = CreateMessageContext(route, message, options);
        await Topology.Dispatch.PassAsync(ctx, token);
    }

    public async Task<TResult> SendAsync<TMessage, TResult>(TMessage message, Options? options = null,
        CancellationToken token = default)
    {
        options ??= new Options();
        var route = Route.For<TMessage, TResult>(options.RoutingKey);
        var ctx = CreateMessageContext(route, message, options);
        return await Topology.Dispatch.PassAsync<TMessage, TResult>(ctx, token);
    }

    public MediatorTopology Topology { get; }

    public ValueTask DisposeAsync() => Topology.DisposeAsync();

    private static MessageContext<TMessage> CreateMessageContext<TMessage>(Route route, TMessage message,
        Options options)
    {
        var meta = options.Meta.ToImmutableDictionary();
        var extra = options.Extra.ToImmutableDictionary();
        var ctx = new MessageContext<TMessage>(route, message, meta, extra) { CreatedAt = DateTime.UtcNow };
        return ctx;
    }
}