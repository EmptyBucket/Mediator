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
    public MediatorTopology(IConnectingPipe dispatch, IConnectingPipe receive, IPipeFactory pipeFactory)
    {
        Dispatch = dispatch;
        Receive = receive;
        PipeFactory = pipeFactory;
    }

    public void Deconstruct(out IConnectingPipe dispatch, out IConnectingPipe receive)
    {
        dispatch = Dispatch;
        receive = Receive;
    }
    
    public void Deconstruct(out IConnectingPipe dispatch, out IConnectingPipe receive, out IPipeFactory pipeFactory)
    {
        dispatch = Dispatch;
        receive = Receive;
        pipeFactory = PipeFactory;
    }

    /// <summary>
    /// Connecting pipe to connect the dispatch topology
    /// </summary>
    public IConnectingPipe Dispatch { get; set; }

    /// <summary>
    /// Connecting pipe to connect the receive topology
    /// </summary>
    public IConnectingPipe Receive { get; set; }
    
    public IPipeFactory PipeFactory { get; }

    public async ValueTask DisposeAsync()
    {
        await Dispatch.DisposeAsync();
        await Receive.DisposeAsync();
    }
}