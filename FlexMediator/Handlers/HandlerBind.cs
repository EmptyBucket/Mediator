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

namespace FlexMediator.Handlers;

public readonly record struct HandlerBind : IDisposable
{
    private readonly Action<HandlerBind> _unbind;

    public HandlerBind(Action<HandlerBind> unbind, Route route, Type? handlerType = null, IHandler? handler = null)
    {
        if (handlerType is null && handler is null)
            throw new ArgumentException($"{nameof(handlerType)} or {nameof(handler)} must be not-null");

        _unbind = unbind;

        Route = route;
        Handler = handler;
        HandlerType = handlerType;
    }

    public Route Route { get; }

    public Type? HandlerType { get; }
    
    public IHandler? Handler { get; }

    public void Dispose() => _unbind(this);
}