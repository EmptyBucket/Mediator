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

using FlexMediator.Pipes;

namespace FlexMediator.Plumbers;

public class Plumber : IPlumber
{
    private readonly IPipeConnector _pipeConnector;
    private readonly IPipeFactory _pipeFactory;
    private readonly List<PipeConnection> _pipeConnections = new();

    public Plumber(IPipeConnector pipeConnector, IPipeFactory pipeFactory)
    {
        _pipeConnector = pipeConnector;
        _pipeFactory = pipeFactory;
    }

    public async Task<IPlumber> Connect<TMessage, TPipe>(string routingKey = "", Action<IPipeConnector>? make = null)
        where TPipe : IPipe, IPipeConnector
    {
        var nextPipe = _pipeFactory.Create<TPipe>();
        make?.Invoke(nextPipe);

        var pipeConnection = await _pipeConnector.Connect<TMessage>(nextPipe, routingKey);
        _pipeConnections.Add(pipeConnection);

        return this;
    }

    public async Task<IPlumber> Connect<TMessage, TResult, TPipe>(string routingKey = "",
        Action<IPipeConnector>? make = null)
        where TPipe : IPipe, IPipeConnector
    {
        var nextPipe = _pipeFactory.Create<TPipe>();
        make?.Invoke(nextPipe);

        var pipeConnection = await _pipeConnector.Connect<TMessage, TResult>(nextPipe, routingKey);
        _pipeConnections.Add(pipeConnection);

        return this;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections) await pipeConnection.DisposeAsync();
    }
}