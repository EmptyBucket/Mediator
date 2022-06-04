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

namespace Mediator.Pipes.Utils;

public class PipeConnection<TPipe> : IDisposable, IAsyncDisposable
{
    private int _isDisposed;
    private readonly Action<PipeConnection<TPipe>> _disconnect;
    private readonly Func<PipeConnection<TPipe>, ValueTask> _disconnectAsync;

    public PipeConnection(Route route, TPipe pipe,
        Action<PipeConnection<TPipe>> disconnect, Func<PipeConnection<TPipe>, ValueTask> disconnectAsync)
    {
        Route = route;
        Pipe = pipe;
        _disconnect = disconnect;
        _disconnectAsync = disconnectAsync;
    }

    public PipeConnection(Route route, TPipe pipe, Action<PipeConnection<TPipe>> disconnect)
        : this(route, pipe, disconnect, DisconnectAsync(disconnect))
    {
    }

    private static Func<PipeConnection<TPipe>, ValueTask> DisconnectAsync(Action<PipeConnection<TPipe>> disconnect) =>
        p =>
        {
            disconnect(p);
            return new ValueTask();
        };

    public Route Route { get; }

    public TPipe Pipe { get; }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0) _disconnect(this);
    }

    public ValueTask DisposeAsync() =>
        Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0 ? _disconnectAsync(this) : new ValueTask();
}