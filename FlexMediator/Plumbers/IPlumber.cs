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
using FlexMediator.Pipes;

namespace FlexMediator.Plumbers;

public interface IPlumber
{
    Task<IPlumber> Connect<TMessage, TPipe>(string routingKey = "",
        Action<IPlumber<TMessage>>? next = null)
        where TPipe : IPipe, IPipeConnector;

    Task<IPlumber> Connect<TMessage, TResult, TPipe>(string routingKey = "",
        Action<IPlumber<TMessage, TResult>>? next = null)
        where TPipe : IPipe, IPipeConnector;

    Task<IPlumber> ConnectHandlers<TMessage>(string routingKey = "",
        Action<IHandlerPlumber<TMessage>>? next = null);

    Task<IPlumber> ConnectHandlers<TMessage, TResult>(string routingKey = "",
        Action<IHandlerPlumber<TMessage, TResult>>? next = null);
}

public interface IPlumber<TMessage>
{
    Task<IPlumber<TMessage>> Connect<TPipe>(string routingKey = "",
        Action<IPlumber<TMessage>>? next = null)
        where TPipe : IPipe, IPipeConnector;

    Task<IPlumber<TMessage>> ConnectHandlers(string routingKey = "",
        Action<IHandlerPlumber<TMessage>>? next = null);
}

public interface IPlumber<TMessage, TResult>
{
    Task<IPlumber<TMessage, TResult>> Connect<TPipe>(string routingKey = "",
        Action<IPlumber<TMessage, TResult>>? next = null)
        where TPipe : IPipe, IPipeConnector;

    Task<IPlumber<TMessage, TResult>> ConnectHandlers(string routingKey = "",
        Action<IHandlerPlumber<TMessage, TResult>>? next = null);
}

public interface IHandlerPlumber<TMessage>
{
    IHandlerPlumber<TMessage> Connect(Func<IServiceProvider, IHandler> factory, string routingKey = "");
}

public interface IHandlerPlumber<TMessage, TResult>
{
    IHandlerPlumber<TMessage, TResult> Connect(Func<IServiceProvider, IHandler> factory, string routingKey = "");
}