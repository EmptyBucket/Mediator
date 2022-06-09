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
    public string? MessageId
    {
        get => Get<string>(nameof(MessageId));
        set => Set(nameof(MessageId), value);
    }

    public string? CorrelationId
    {
        get => Get<string>(nameof(CorrelationId));
        set => Set(nameof(CorrelationId), value);
    }

    public string RoutingKey
    {
        get => Get<string>(nameof(RoutingKey), true) ?? string.Empty;
        set => Set(nameof(RoutingKey), value, true);
    }

    public T? Get<T>(string propertyName, bool isExtra = false) =>
        isExtra ? (T?)ExtraProps.GetValueOrDefault(propertyName) : (T?)ServiceProps.GetValueOrDefault(propertyName);

    public void Set<T>(string propertyName, T value, bool isExtra = false)
    {
        if (isExtra) ExtraProps[propertyName] = value;
        else ServiceProps[propertyName] = value;
    }

    /// <summary>
    /// Service information is embedded in the fields of the same name 
    /// </summary>
    public Dictionary<string, object?> ServiceProps { get; set; } = new();

    /// <summary>
    /// Additional information stored in the body 
    /// </summary>
    public Dictionary<string, object?> ExtraProps { get; set; } = new();
}