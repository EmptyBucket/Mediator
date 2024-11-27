using System;
using System.Collections.Generic;
using FluentAssertions;
using Mediator.Configurations;
using Mediator.Pipes;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Mediator.Tests;

public class PipeProviderTests
{
    private ServiceProvider _serviceProvider;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var serviceCollection = new ServiceCollection();
        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    [Test]
    public void Get_WhenBindByInterface_ReturnInstanceOfType()
    {
        var pipeInterface = typeof(IPipe);
        var pipeType = typeof(BroadcastPipe);
        var pipeBinds = new Dictionary<PipeBind, Type> { { new PipeBind(pipeInterface, string.Empty), pipeType } };
        var pipeProvider = new PipeProvider(pipeBinds, _serviceProvider);

        var pipe = pipeProvider.Get<IPipe>();

        pipe.Should().BeOfType(pipeType);
    }

    [Test]
    public void Get_WhenBindByInterface_ReturnInstanceOfGenericType()
    {
        var pipeInterface = typeof(IPipe);
        var pipeType = typeof(GenericPipe<int>);
        var pipeBinds = new Dictionary<PipeBind, Type> { { new PipeBind(pipeInterface, string.Empty), pipeType } };
        var pipeProvider = new PipeProvider(pipeBinds, _serviceProvider);

        var pipe = pipeProvider.Get<IPipe>();

        pipe.Should().BeOfType(pipeType);
    }

    [Test]
    public void Get_WhenBindByType_ReturnInstanceOfType()
    {
        var pipeType = typeof(BroadcastPipe);
        var pipeBinds = new Dictionary<PipeBind, Type> { { new PipeBind(pipeType, string.Empty), pipeType } };
        var pipeProvider = new PipeProvider(pipeBinds, _serviceProvider);

        var pipe = pipeProvider.Get<BroadcastPipe>();

        pipe.Should().BeOfType(pipeType);
    }

    [Test]
    public void Get_WhenBindByGenericType_ReturnInstanceOfGenericType()
    {
        var pipeType = typeof(GenericPipe<int>);
        var pipeBinds = new Dictionary<PipeBind, Type> { { new PipeBind(pipeType, string.Empty), pipeType } };
        var pipeProvider = new PipeProvider(pipeBinds, _serviceProvider);

        var pipe = pipeProvider.Get<GenericPipe<int>>();

        pipe.Should().BeOfType(pipeType);
    }

    [Test]
    public void Get_WhenBindByGenericDefinitionType_ReturnInstanceOfGenericType()
    {
        var pipeType = typeof(GenericPipe<>);
        var pipeBinds = new Dictionary<PipeBind, Type> { { new PipeBind(pipeType, string.Empty), pipeType } };
        var pipeProvider = new PipeProvider(pipeBinds, _serviceProvider);

        var pipe = pipeProvider.Get<GenericPipe<int>>();

        pipe.Should().BeOfType(typeof(GenericPipe<int>));
    }

    [Test]
    public void Get_WhenTwoBindByInterfaceAndType_ReturnSameInstanceOfType()
    {
        var pipeInterface = typeof(IPipe);
        var pipeType = typeof(BroadcastPipe);
        var pipeBinds = new Dictionary<PipeBind, Type>
        {
            { new PipeBind(pipeInterface, string.Empty), pipeType },
            { new PipeBind(pipeType, string.Empty), pipeType }
        };
        var pipeProvider = new PipeProvider(pipeBinds, _serviceProvider);

        var firstPipe = pipeProvider.Get<IPipe>();
        var secondPipe = pipeProvider.Get<BroadcastPipe>();

        firstPipe.Should().BeOfType(pipeType);
        secondPipe.Should().BeOfType(pipeType);
        firstPipe.Should().Be(secondPipe);
    }

    [Test]
    public void Get_WhenTwoBindByTypeWithDiffName_ReturnDiffInstancesOfType()
    {
        var pipeType = typeof(BroadcastPipe);
        var pipeBinds = new Dictionary<PipeBind, Type>
        {
            { new PipeBind(pipeType, "first"), pipeType },
            { new PipeBind(pipeType, "second"), pipeType }
        };
        var pipeProvider = new PipeProvider(pipeBinds, _serviceProvider);

        var firstPipe = pipeProvider.Get<BroadcastPipe>("first");
        var secondPipe = pipeProvider.Get<BroadcastPipe>("second");

        firstPipe.Should().BeOfType(pipeType);
        secondPipe.Should().BeOfType(pipeType);
        firstPipe.Should().NotBe(secondPipe);
    }
}