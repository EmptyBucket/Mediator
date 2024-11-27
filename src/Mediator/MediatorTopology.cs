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

using Mediator.Pipes;

namespace Mediator;

/// <summary>
/// Represents the mediator topology according to which messages are routed
/// </summary>
public class MediatorTopology : IAsyncDisposable
{
    private int _isDisposed;

    public MediatorTopology(IMulticastPipe dispatchPipe, IMulticastPipe receivePipe, IPipeProvider pipeProvider)
    {
        Dispatch = dispatchPipe;
        Receive = receivePipe;
        PipeProvider = pipeProvider;
    }

    public void Deconstruct(out IMulticastPipe dispatchPipe, out IMulticastPipe receivePipe)
    {
        dispatchPipe = Dispatch;
        receivePipe = Receive;
    }

    public void Deconstruct(out IMulticastPipe dispatchPipe, out IMulticastPipe receivePipe,
        out IPipeProvider pipeProvider)
    {
        dispatchPipe = Dispatch;
        receivePipe = Receive;
        pipeProvider = PipeProvider;
    }

    /// <summary>
    /// Connecting pipe to connect the dispatch topology
    /// </summary>
    public IMulticastPipe Dispatch { get; set; }

    /// <summary>
    /// Connecting pipe to connect the receive topology
    /// </summary>
    public IMulticastPipe Receive { get; set; }

    public IPipeProvider PipeProvider { get; }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) return;

        await Dispatch.DisposeAsync();
        await Receive.DisposeAsync();
    }
}