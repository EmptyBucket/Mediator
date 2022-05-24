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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mediator.Configurations;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Action<IPipeBinder>? bindPipes = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        AddPipeFactory(serviceCollection, bindPipes);

        serviceCollection.TryAdd(new ServiceDescriptor(typeof(IMediator),
            p => new MediatorFactory(p).CreateAsync().Result, lifetime));

        return serviceCollection;
    }

    public static IServiceCollection AddMediatorFactory(this IServiceCollection serviceCollection,
        Action<IPipeBinder>? bindPipes = null, Func<IServiceProvider, MediatorTopology, Task>? connectPipes = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        AddPipeFactory(serviceCollection, bindPipes);

        serviceCollection.TryAdd(new ServiceDescriptor(typeof(IMediatorFactory),
            p => new MediatorFactory(p, connectPipes), lifetime));

        return serviceCollection;
    }

    private static void AddPipeFactory(IServiceCollection serviceCollection, Action<IPipeBinder>? bindPipes = null)
    {
        bindPipes ??= _ => { };
        var pipeBinder = new PipeBinder();
        BindDefaultPipes(pipeBinder);
        bindPipes(pipeBinder);
        var pipeBinds = pipeBinder.Build();
        serviceCollection.TryAddScoped<IPipeFactory>(p => new PipeFactory(pipeBinds, p));
    }

    private static void BindDefaultPipes(IPipeBinder pipeBinder) =>
        pipeBinder
            .Bind(typeof(HandlingPipe<>))
            .BindInterfaces(typeof(HandlingPipe<>), "HandlingPipe<>")
            .Bind(typeof(HandlingPipe<,>))
            .BindInterfaces(typeof(HandlingPipe<,>), "HandlingPipe<,>")
            .Bind<Pipe>()
            .BindInterfaces<Pipe>(nameof(Pipe));
}