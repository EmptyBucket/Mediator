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

namespace Mediator.Handlers;

/// <summary>
/// Represents the handler interface
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public interface IHandler<TMessage>
{
    /// <summary>
    /// Processes received message inside <paramref name="ctx"/>
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task HandleAsync(MessageContext<TMessage> ctx, CancellationToken cancellationToken);
}

/// <summary>
/// Represents the handler interface
/// </summary>
/// <typeparam name="TMessage"></typeparam>
/// <typeparam name="TResult"></typeparam>
public interface IHandler<TMessage, TResult> : IHandler<TMessage>
{
    /// <summary>
    /// Processes received message inside <paramref name="ctx"/> and returns a result of type <typeparamref name="TResult"/>
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    new Task<TResult> HandleAsync(MessageContext<TMessage> ctx, CancellationToken cancellationToken);

    /// <summary>
    /// Processes received message inside <paramref name="ctx"/>
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task IHandler<TMessage>.HandleAsync(MessageContext<TMessage> ctx, CancellationToken cancellationToken) =>
        HandleAsync(ctx, cancellationToken);
}