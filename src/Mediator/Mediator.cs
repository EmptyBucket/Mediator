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

/// <summary>
/// Represents the mediator thar will deliver a message according to the built topology
/// </summary>
internal class Mediator : IMediator
{
    public Mediator(MediatorTopology topology)
    {
        Topology = topology;
    }

    /// <inheritdoc />
    public Task PublishAsync<TMessage>(TMessage message, Options? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new Options();
        var route = Route.For<TMessage>(options.RoutingKey);
        var ctx = CreateMessageContext(route, message, options);
        return Topology.Dispatch.PassAsync(ctx, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TResult> SendAsync<TMessage, TResult>(TMessage message, Options? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new Options();
        var route = Route.For<TMessage, TResult>(options.RoutingKey);
        var ctx = CreateMessageContext(route, message, options);
        return Topology.Dispatch.PassAsync<TMessage, TResult>(ctx, cancellationToken);
    }

    /// <inheritdoc />
    public MediatorTopology Topology { get; }

    public ValueTask DisposeAsync() => Topology.DisposeAsync();

    private static MessageContext<TMessage> CreateMessageContext<TMessage>(Route route, TMessage message,
        Options options)
    {
        var serviceProps = options.ServiceProps.ToImmutableDictionary();
        var extraProps = options.ExtraProps.ToImmutableDictionary();
        var ctx = new MessageContext<TMessage>(route, message, serviceProps, extraProps)
            { CreatedAt = DateTime.UtcNow };
        return ctx;
    }
}