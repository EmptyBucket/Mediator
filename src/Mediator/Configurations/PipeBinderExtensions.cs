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

public static class PipeBinderExtensions
{
    /// <summary>
    /// Bind <paramref name="pipeType"/> to itself with <paramref name="pipeName"/>
    /// </summary>
    /// <param name="pipeBinder"></param>
    /// <param name="pipeType"></param>
    /// <param name="pipeName"></param>
    /// <returns></returns>
    public static IPipeBinder Bind(this IPipeBinder pipeBinder, Type pipeType, string pipeName = "") =>
        pipeBinder.Bind(pipeType, pipeType, pipeName);

    /// <summary>
    /// Bind <typeparamref name="TPipe" /> to itself with <paramref name="pipeName"/>
    /// </summary>
    /// <param name="pipeBinder"></param>
    /// <param name="pipeName"></param>
    /// <typeparam name="TPipe"></typeparam>
    /// <returns></returns>
    public static IPipeBinder Bind<TPipe>(this IPipeBinder pipeBinder, string pipeName = "") =>
        pipeBinder.Bind(typeof(TPipe), pipeName);

    /// <summary>
    /// Bind <paramref name="pipeType"/> to all its interfaces with <paramref name="pipeName"/>
    /// </summary>
    /// <param name="pipeBinder"></param>
    /// <param name="pipeType"></param>
    /// <param name="pipeName"></param>
    /// <returns></returns>
    public static IPipeBinder BindInterfaces(this IPipeBinder pipeBinder, Type pipeType, string pipeName) =>
        pipeType.GetInterfaces()
            .Where(i => typeof(IPubPipe).IsAssignableFrom(i) || typeof(IReqPipe).IsAssignableFrom(i))
            .Aggregate(pipeBinder, (b, i) => b.Bind(i, pipeType, pipeName));

    /// <summary>
    /// Bind <typeparamref name="TPipe"/> to all its interfaces with <paramref name="pipeName"/>
    /// </summary>
    /// <param name="pipeBinder"></param>
    /// <param name="pipeName"></param>
    /// <typeparam name="TPipe"></typeparam>
    /// <returns></returns>
    public static IPipeBinder BindInterfaces<TPipe>(this IPipeBinder pipeBinder, string pipeName) =>
        pipeBinder.BindInterfaces(typeof(TPipe), pipeName);
}