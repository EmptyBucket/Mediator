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
using ConsoleApp5.Pipes;

namespace ConsoleApp5.Registries;

public class TransportRegistry : ITransportRegistry, ITransportProvider
{
    private readonly ServiceFactory _serviceFactory;
    private readonly ConcurrentDictionary<string, Type> _transportTypes = new();

    public TransportRegistry(ServiceFactory serviceFactory)
    {
        _serviceFactory = serviceFactory;
    }

    public void AddTransport<TPipe>(string name) where TPipe : IPipe
    {
        _transportTypes.TryAdd(name, typeof(TPipe));
    }

    public void RemoveTransport(string name)
    {
        _transportTypes.TryRemove(name, out _);
    }

    public IPipe GetTransport(string name)
    {
        return (IPipe)_serviceFactory(_transportTypes[name]);
    }
}