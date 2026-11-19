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
using Mediator.Pipes.Utils;

namespace Mediator.Handlers;

/// <inheritdoc />
public record MessageContext<TMessage>(
    Route Route,
    TMessage Message,
    IImmutableDictionary<string, string?> ServiceProps,
    IImmutableDictionary<string, string?> ExtraProps)
    : SerializableMessageContext<TMessage>(Route, Message, ServiceProps, ExtraProps)
{
    /// <summary>
    /// Unique message id
    /// </summary>
    public string? MessageId
    {
        get => ServiceProps.GetValueOrDefault(nameof(MessageId));
        init => ServiceProps = ServiceProps.SetItem(nameof(MessageId), value);
    }

    /// <summary>
    /// Unique message correlation id
    /// </summary>
    public string? CorrelationId
    {
        get => ServiceProps.GetValueOrDefault(nameof(CorrelationId));
        init => ServiceProps = ServiceProps.SetItem(nameof(CorrelationId), value);
    }

    /// <summary>
    /// The time the message was created
    /// </summary>
    public DateTime? CreatedAt
    {
        get => ExtraProps.GetValueOrDefault(nameof(CreatedAt)) is { } value ? DateTime.Parse(value) : null;
        init => ExtraProps = ExtraProps.SetItem(nameof(CreatedAt), value.ToString());
    }

    /// <summary>
    /// The time the message was delivered
    /// </summary>
    public DateTime? DeliveredAt
    {
        get => ExtraProps.GetValueOrDefault(nameof(DeliveredAt)) is { } value ? DateTime.Parse(value) : null;
        init => ExtraProps = ExtraProps.SetItem(nameof(DeliveredAt), value.ToString());
    }

    /// <summary>
    /// The time it took for the message to be delivered
    /// </summary>
    public TimeSpan? DeliveryTime =>
        CreatedAt != null && DeliveredAt != null ? DeliveredAt.Value - CreatedAt.Value : null;

    public IServiceProvider? ServiceProvider { get; init; }
}