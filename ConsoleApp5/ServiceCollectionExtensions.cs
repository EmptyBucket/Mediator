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

using ConsoleApp5.Topologies;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp5;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Action<IMediator>? mediatorBuilder = null, ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        var serviceDescriptor = new ServiceDescriptor(typeof(IMediator), p =>
        {
            var topologyRegistry = new DirectTopologyRegistry();
            var pipeRegistry = new TransportRegistry(p);
            var mediator = new Mediator();
            mediator.AddDefaultTransport();
            mediatorBuilder?.Invoke(mediator);
            return mediator;
        }, lifetime);
        serviceCollection.Add(serviceDescriptor);

        return serviceCollection;
    }
}