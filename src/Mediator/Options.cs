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

namespace Mediator;

public class Options
{
    /// <summary>
    /// Unique message id
    /// </summary>
    public string? MessageId
    {
        get => ServiceProps.GetValueOrDefault(nameof(MessageId));
        set => ServiceProps[nameof(MessageId)] = value;
    }

    /// <summary>
    /// Unique message correlation id
    /// </summary>
    public string? CorrelationId
    {
        get => ServiceProps.GetValueOrDefault(nameof(CorrelationId));
        set => ServiceProps[nameof(CorrelationId)] = value;
    }

    /// <summary>
    /// RoutingKey for routing this message, according to the built topology
    /// </summary>
    public string RoutingKey
    {
        get => ExtraProps.GetValueOrDefault(nameof(RoutingKey)) ?? string.Empty;
        set => ExtraProps[nameof(RoutingKey)] = value;
    }

    /// <summary>
    /// Service properties for embedding in infrastructure fields of the same name, e.g. in redis transport message fields
    /// </summary>
    public Dictionary<string, string?> ServiceProps { get; set; } = new();

    /// <summary>
    /// Extra properties for users needed stored in body of message
    /// </summary>
    public Dictionary<string, string?> ExtraProps { get; set; } = new();
}