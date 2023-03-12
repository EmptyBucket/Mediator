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
using System.Text.Json.Serialization;
using Mediator.Pipes.Utils;

namespace Mediator.Handlers;

/// <summary>
/// Represents the message context
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public record MessageContext<TMessage>
{
    private IImmutableDictionary<string, object?> _serviceProps;
    private IImmutableDictionary<string, object?> _extraProps;

    [JsonConstructor]
    [Newtonsoft.Json.JsonConstructor]
    public MessageContext(Route route, TMessage message, IImmutableDictionary<string, object?> serviceProps,
        IImmutableDictionary<string, object?> extraProps)
    {
        Route = route;
        Message = message;
        _serviceProps = serviceProps;
        _extraProps = extraProps;
    }

    public MessageContext(MessageContext<TMessage> ctx)
    {
        Route = ctx.Route;
        Message = ctx.Message;
        _serviceProps = ctx._serviceProps;
        _extraProps = ctx._extraProps;
    }

    /// <summary>
    /// Route for routing this message, according to the built topology
    /// </summary>
    public Route Route { get; init; }

    /// <summary>
    /// Message
    /// </summary>
    public TMessage Message { get; init; }

    /// <summary>
    /// Unique message id
    /// </summary>
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string? MessageId
    {
        get => Get<string>(nameof(MessageId));
        init => Set(nameof(MessageId), value);
    }

    /// <summary>
    /// Unique message correlation id
    /// </summary>
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string? CorrelationId
    {
        get => Get<string>(nameof(CorrelationId));
        init => Set(nameof(CorrelationId), value);
    }

    /// <summary>
    /// The time the message was created
    /// </summary>
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public DateTime CreatedAt
    {
        get => Get<DateTime>(nameof(CreatedAt), true);
        init => Set(nameof(CreatedAt), value, true);
    }

    /// <summary>
    /// The time the message was delivered
    /// </summary>
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public DateTime? DeliveredAt
    {
        get => Get<DateTime>(nameof(DeliveredAt), true);
        init => Set(nameof(DeliveredAt), value, true);
    }

    /// <summary>
    /// The time it took for the message to be delivered
    /// </summary>
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public TimeSpan? DeliveryTime => DeliveredAt is not null ? DeliveredAt.Value - CreatedAt : null;

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public IServiceProvider? ServiceProvider { get; init; }

    /// <summary>
    /// Get property by <paramref name="propertyName"/>
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="isExtra"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T? Get<T>(string propertyName, bool isExtra = false) =>
        isExtra ? (T?)ExtraProps.GetValueOrDefault(propertyName) : (T?)ServiceProps.GetValueOrDefault(propertyName);

    /// <summary>
    /// Set property by <paramref name="propertyName"/>
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="value"></param>
    /// <param name="isExtra"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private void Set<T>(string propertyName, T value, bool isExtra = false)
    {
        if (isExtra) _extraProps = _extraProps.SetItem(propertyName, value);
        else _serviceProps = _serviceProps.SetItem(propertyName, value);
    }

    /// <summary>
    /// Service properties for embedding in infrastructure fields of the same name, e.g. in redis transport message fields
    /// </summary>
    public IImmutableDictionary<string, object?> ServiceProps
    {
        get => _serviceProps;
        init => _serviceProps = value;
    }

    /// <summary>
    /// Extra properties for users needed stored in body of message
    /// </summary>
    public IImmutableDictionary<string, object?> ExtraProps
    {
        get => _extraProps;
        init => _extraProps = value;
    }
}