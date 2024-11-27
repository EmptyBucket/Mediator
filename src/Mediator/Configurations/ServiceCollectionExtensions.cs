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
        Action<IServiceProvider, MediatorTopology>? connectPipes = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton) =>
        serviceCollection.AddMediator(null, connectPipes, lifetime);

    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Action<IPipeBinder>? bindPipes, Action<IServiceProvider, MediatorTopology>? connectPipes = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        if (bindPipes is not null) serviceCollection.AddSingleton(new BindPipes(bindPipes));

        if (connectPipes is not null) serviceCollection.AddSingleton(new ConnectPipes(connectPipes));

        TryAddPipeBinder(serviceCollection);
        TryAddPipeProvider(serviceCollection, lifetime);
        TryAddMediatorTopology(serviceCollection, lifetime);
        TryAddMediator(serviceCollection, lifetime);
        serviceCollection.AddHostedService<MediatorHostedService>();

        return serviceCollection;
    }

    private static void TryAddMediator(IServiceCollection serviceCollection, ServiceLifetime lifetime) =>
        serviceCollection.TryAdd(new ServiceDescriptor(typeof(IMediator), p =>
        {
            var mediatorTopology = p.GetRequiredService<MediatorTopology>();

            return new Mediator(mediatorTopology);
        }, lifetime));

    private static void TryAddMediatorTopology(IServiceCollection serviceCollection, ServiceLifetime lifetime) =>
        serviceCollection.TryAdd(new ServiceDescriptor(typeof(MediatorTopology), p =>
        {
            var dispatchPipe = new MulticastPipe(p);
            var receivePipe = new MulticastPipe(p);
            var pipeProvider = p.GetRequiredService<IPipeProvider>();
            var mediatorTopology = new MediatorTopology(dispatchPipe, receivePipe, pipeProvider);

            var connect = p.GetServices<ConnectPipes>().Aggregate(new ConnectPipes((_, _) => { }), (a, n) => a + n);
            connect(p, mediatorTopology);

            return mediatorTopology;
        }, lifetime));

    private static void TryAddPipeProvider(IServiceCollection serviceCollection, ServiceLifetime lifetime) =>
        serviceCollection.TryAdd(new ServiceDescriptor(typeof(IPipeProvider), p =>
        {
            var pipeBinds = p.GetRequiredService<PipeBinder>().Build();

            return new PipeProvider(pipeBinds, p);
        }, lifetime));

    private static void TryAddPipeBinder(IServiceCollection serviceCollection) =>
        serviceCollection.TryAddSingleton(p =>
        {
            var pipeBinder = new PipeBinder();

            var bind = p.GetServices<BindPipes>().Aggregate(new BindPipes(_ => { }), (a, n) => a + n);
            bind(pipeBinder);

            return pipeBinder;
        });

    private delegate void BindPipes(IPipeBinder pipeBinder);

    private delegate void ConnectPipes(IServiceProvider serviceProvider, MediatorTopology mediatorTopology);
}