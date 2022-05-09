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

namespace Mediator;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        ServiceLifetime lifetime = ServiceLifetime.Singleton) =>
        AddMediator(serviceCollection, (IServiceProvider _, IPipeConnector _) => Task.CompletedTask, lifetime);

    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Func<IPipeFactory, IPipeConnector, Task> builder, ServiceLifetime lifetime = ServiceLifetime.Singleton) =>
        AddMediator(serviceCollection, (s, p) => builder(s.GetRequiredService<IPipeFactory>(), p), lifetime);

    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Func<IServiceProvider, IPipeConnector, Task> builder, ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        serviceCollection.AddTransient(typeof(HandlingPipe<>));
        serviceCollection.AddTransient(typeof(HandlingPipe<,>));
        serviceCollection.AddTransient<BranchingPipe>();
        serviceCollection.AddSingleton<IPipeFactory, PipeFactory>();

        serviceCollection.Add(
            new ServiceDescriptor(typeof(IMediatorFactory), p => new MediatorFactory(builder, p), lifetime));

        return serviceCollection;
    }
}