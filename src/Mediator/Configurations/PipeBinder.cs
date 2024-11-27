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

namespace Mediator.Configurations;

/// <summary>
/// Represents the builder for building pipe bindings
/// </summary>
internal class PipeBinder : IPipeBinder
{
    private readonly Dictionary<PipeBind, Type> _pipeBinds = new();

    /// <inheritdoc />
    public IPipeBinder Bind(Type pipeType, Type pipeImplType, string pipeName = "")
    {
        if (!typeof(IPubPipe).IsAssignableFrom(pipeType) && !typeof(IReqPipe).IsAssignableFrom(pipeType))
            throw new ArgumentException($"{pipeType} must be {nameof(IPubPipe)} or {nameof(IReqPipe)}");

        if (!typeof(IPubPipe).IsAssignableFrom(pipeImplType) && !typeof(IReqPipe).IsAssignableFrom(pipeImplType))
            throw new ArgumentException($"{pipeImplType} must be {nameof(IPubPipe)} or {nameof(IReqPipe)}");

        _pipeBinds[new PipeBind(pipeType, pipeName)] = pipeImplType;

        return this;
    }

    /// <summary>
    /// Build pipe bindings
    /// </summary>
    /// <returns></returns>
    public IReadOnlyDictionary<PipeBind, Type> Build() => _pipeBinds;
}