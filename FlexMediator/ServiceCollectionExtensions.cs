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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlexMediator;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection serviceCollection,
        Action<MediatorConfiguration>? mediatorBuilder = null, ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        var serviceDescriptor = new ServiceDescriptor(typeof(IMediator), p =>
        {
            var directTopology = ActivatorUtilities.CreateInstance<DirectTopologyFactory>(p, dispatchPipeBinder)
                .Create();
            var rabbitMqTopology = ActivatorUtilities.CreateInstance<RabbitMqTopologyFactory>(p, dispatchPipeBinder)
                .Create();
            var topologies = new Dictionary<string, RabbitMqTopologyBinder>
            {
                { "direct", directTopology },
                { "rabbitmq", rabbitMqTopology }
            };
            mediatorBuilder?.Invoke(mediator.Configuration);
            return mediator;
        }, lifetime);
        serviceCollection.Add(serviceDescriptor);

        return serviceCollection;
    }
}

public class MediatorBuilder
{
    private readonly ServiceCollection _serviceCollection;
    private readonly Dictionary<string, Func<IServiceProvider, RabbitMqTopologyBinder>> _topologyFactories = new();

    public MediatorBuilder(ServiceCollection serviceCollection)
    {
        _serviceCollection = new ServiceCollection { serviceCollection };
    }

    public MediatorBuilder AddTopology(string name, Func<IServiceProvider, RabbitMqTopologyBinder> topologyFactory)
    {
        _topologyFactories[name] = topologyFactory;
        return this;
    }

    public IMediator Build()
    {
        var dispatchPipeBinder = new PipeBinder();
        var dispatchPipe = new BranchingPipe(dispatchPipeBinder);

        var mediatorConfiguration = new MediatorConfiguration();
        var mediator = new Mediator(dispatchPipe, mediatorConfiguration);
        return mediator;
    }
}