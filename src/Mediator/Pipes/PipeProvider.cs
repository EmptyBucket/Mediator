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

using System.Collections.Concurrent;
using Mediator.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Pipes;

/// <summary>
/// Represents the pipe provider to create or provide existing pipes
/// </summary>
internal class PipeProvider : IPipeProvider
{
    private readonly IReadOnlyDictionary<PipeBind, Type> _pipeBinds;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<PipeBind, object> _pipes = new();

    public PipeProvider(IReadOnlyDictionary<PipeBind, Type> pipeBinds, IServiceProvider serviceProvider)
    {
        _pipeBinds = pipeBinds;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public TPipe Get<TPipe>(string pipeName = "")
    {
        var pipeType = typeof(TPipe);

        if (!typeof(IPubPipe).IsAssignableFrom(pipeType) && !typeof(IReqPipe).IsAssignableFrom(pipeType))
            throw new ArgumentException($"{pipeType} must be {nameof(IPubPipe)} or {nameof(IReqPipe)}");

        if (_pipeBinds.TryGetValue(new PipeBind(pipeType, pipeName), out var pipeImplType))
            return (TPipe)_pipes.GetOrAdd(new PipeBind(pipeImplType, pipeName),
                _ => ActivatorUtilities.CreateInstance(_serviceProvider, pipeImplType));

        if (pipeType.IsGenericType &&
            _pipeBinds.TryGetValue(new PipeBind(pipeType.GetGenericTypeDefinition(), pipeName), out pipeImplType))
        {
            pipeImplType = pipeImplType.MakeGenericType(pipeType.GetGenericArguments());
            return (TPipe)_pipes.GetOrAdd(new PipeBind(pipeImplType, pipeName),
                _ => ActivatorUtilities.CreateInstance(_serviceProvider, pipeImplType));
        }

        throw new InvalidOperationException($"{typeof(TPipe)} was not bounded");
    }
}