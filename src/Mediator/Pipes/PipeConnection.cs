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

namespace Mediator.Pipes;

public record struct PipeConnection : IAsyncDisposable
{
    private int _isDisposed = 0;
    private readonly Func<PipeConnection, ValueTask> _disconnect;

    public PipeConnection(Route route, IPipe pipe, Func<PipeConnection, ValueTask> disconnect)
    {
        Route = route;
        Pipe = pipe;
        _disconnect = disconnect;
    }

    public Route Route { get; }

    public IPipe Pipe { get; }

    public ValueTask DisposeAsync() => Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1
        ? ValueTask.CompletedTask
        : _disconnect(this);
}