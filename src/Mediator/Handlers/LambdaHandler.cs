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

public class LambdaHandler<TMessage> : IHandler<TMessage>
{
    private readonly Func<MessageContext<TMessage>, CancellationToken, Task> _func;

    public LambdaHandler(Func<MessageContext<TMessage>, CancellationToken, Task> func)
    {
        _func = func;
    }

    public Task HandleAsync(MessageContext<TMessage> ctx, CancellationToken token) => _func(ctx, token);
}

public class LambdaHandler<TMessage, TResult> : IHandler<TMessage, TResult>
{
    private readonly Func<MessageContext<TMessage>, CancellationToken, Task<TResult>> _func;

    public LambdaHandler(Func<MessageContext<TMessage>, CancellationToken, Task<TResult>> func)
    {
        _func = func;
    }

    public Task<TResult> HandleAsync(MessageContext<TMessage> ctx, CancellationToken token) => _func(ctx, token);
}