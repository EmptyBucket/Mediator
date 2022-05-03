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

using System.Data;
using ConsoleApp5.TransportBindings;

namespace ConsoleApp5.Pipes;

public class TransportForkPipe : IPipe
{
    private readonly ITransportBindProvider _transportBindProvider;

    public TransportForkPipe(ITransportBindProvider transportBindProvider)
    {
        _transportBindProvider = transportBindProvider;
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options, CancellationToken token)
    {
        var transportBinds = _transportBindProvider.GetBinds<TMessage>(options.RoutingKey);
        var pipes = transportBinds.Select(t => t.Transport.Pipe);

        await Task.WhenAll(pipes.Select(p => p.Handle(message, options, token)));
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token)
    {
        var transportBinds = _transportBindProvider.GetBinds<TMessage>(options.RoutingKey);
        var pipes = transportBinds.Select(t => t.Transport.Pipe).ToArray();

        if (pipes.Length != 1) throw new InvalidConstraintException("Must be single pipe");

        return await pipes.Single().Handle<TMessage, TResult>(message, options, token);
    }
}