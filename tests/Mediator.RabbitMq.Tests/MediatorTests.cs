using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Management.Client;
using FluentAssertions;
using Mediator.Configurations;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.RabbitMq.Configurations;
using Mediator.RabbitMq.Pipes;
using Mediator.Tests;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace Mediator.RabbitMq.Tests;

public class MediatorTests
{
    private const string RabbitMqEndpoint = "localhost";
    private const string VhostName = "test";

    private ServiceProvider _serviceProvider;
    private IMediator _mediator;
    private Mock<IPipe> _fEndPipe;
    private Mock<IPipe> _sEndPipe;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .RegisterEasyNetQ($"host={RabbitMqEndpoint};virtualHost={VhostName}")
            .AddSingleton<IManagementClient>(new ManagementClient(RabbitMqEndpoint, "guest", "guest"))
            .AddMediator(b => b.BindRabbitMq(), lifetime: ServiceLifetime.Transient);
        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    [SetUp]
    public async Task Setup()
    {
        await DeleteVHostAsync();
        await CreateVHostAsync();

        _mediator = _serviceProvider.GetRequiredService<IMediator>();
        _fEndPipe = new Mock<IPipe>();
        _sEndPipe = new Mock<IPipe>();
    }

    [TearDown]
    public async Task Teardown() => await _mediator.DisposeAsync();

    [OneTimeTearDown]
    public Task OneTimeTeardown() => DeleteVHostAsync();

    private async Task CreateVHostAsync()
    {
        var managementClient = _serviceProvider.GetRequiredService<IManagementClient>();
        await managementClient.CreateVhostAsync(VhostName);
    }

    private async Task DeleteVHostAsync()
    {
        var managementClient = _serviceProvider.GetRequiredService<IManagementClient>();
        var vhost = (await managementClient.GetVhostsAsync()).FirstOrDefault(v => v.Name == VhostName);

        if (vhost is not null) await managementClient.DeleteVhostAsync(vhost);
    }

    [Test]
    public async Task PublishAsync_WhenRabbitMqTopology_CallPassAsync()
    {
        var (dispatch, _, pipeFactory) = _mediator.Topology;
        await using var pipe = pipeFactory.Create<RabbitMqPipe>();
        var someEvent = new SomeEvent(Guid.NewGuid());

        await dispatch.ConnectOutAsync<SomeEvent>(pipe);
        await pipe.ConnectOutAsync<SomeEvent>(_fEndPipe.Object);
        await _mediator.PublishAsync(someEvent);

        await _fEndPipe.VerifyAsync(p => p.PassAsync(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Test]
    public async Task PublishAsync_WhenRabbitMqTopologyWithTwoConsumers_CallPassAsyncOnBoth()
    {
        var (dispatch, _, pipeFactory) = _mediator.Topology;
        await using var pipe = pipeFactory.Create<RabbitMqPipe>();
        var someEvent = new SomeEvent(Guid.NewGuid());

        await dispatch.ConnectOutAsync<SomeEvent>(pipe);
        await pipe.ConnectOutAsync<SomeEvent>(_fEndPipe.Object, subscriptionId: "1");
        await pipe.ConnectOutAsync<SomeEvent>(_sEndPipe.Object, subscriptionId: "2");
        await _mediator.PublishAsync(someEvent);

        await _fEndPipe.VerifyAsync(p => p.PassAsync(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()),
            Times.Once());
        await _sEndPipe.VerifyAsync(p => p.PassAsync(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Test]
    public async Task PublishAsyncTwice_WhenRabbitMqTopologyWithConsumerGroup_CallPassAsyncOnBoth()
    {
        var (dispatch, _, pipeFactory) = _mediator.Topology;
        await using var pipe = pipeFactory.Create<RabbitMqPipe>();
        var someEvent = new SomeEvent(Guid.NewGuid());
        async Task DoSomeWork() => await Task.Delay(1_000);
        _fEndPipe.Setup(p => p.PassAsync(It.IsAny<MessageContext<SomeEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(DoSomeWork);
        _sEndPipe.Setup(p => p.PassAsync(It.IsAny<MessageContext<SomeEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(DoSomeWork);

        await dispatch.ConnectOutAsync<SomeEvent>(pipe);
        await pipe.ConnectOutAsync<SomeEvent>(_fEndPipe.Object, subscriptionId: "1");
        await pipe.ConnectOutAsync<SomeEvent>(_sEndPipe.Object, subscriptionId: "1");
        await _mediator.PublishAsync(someEvent);
        await _mediator.PublishAsync(someEvent);

        await _fEndPipe.VerifyAsync(p => p.PassAsync(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()),
            Times.Once());
        await _sEndPipe.VerifyAsync(p => p.PassAsync(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Test]
    public async Task SendAsync_WhenRabbitMqTopologyWithResult_CallPassAsync()
    {
        var (dispatch, _, pipeFactory) = _mediator.Topology;
        await using var pipe = pipeFactory.Create<RabbitMqPipe>();
        var someEvent = new SomeEvent(Guid.NewGuid());

        await dispatch.ConnectOutAsync<SomeEvent, SomeResult>(pipe);
        await pipe.ConnectOutAsync<SomeEvent, SomeResult>(_fEndPipe.Object);
        await _mediator.SendAsync<SomeEvent, SomeResult>(someEvent);

        _fEndPipe.Verify(p => p.PassAsync<SomeEvent, SomeResult>(
            It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()));
    }

    [Test]
    public async Task SendAsync_WhenRabbitMqTopologyWithResult_ReturnResult()
    {
        var (dispatch, _, pipeFactory) = _mediator.Topology;
        await using var pipe = pipeFactory.Create<RabbitMqPipe>();
        var someEvent = new SomeEvent(Guid.NewGuid());
        var expectedResult = new SomeResult(Guid.NewGuid());
        _fEndPipe.Setup(p => p.PassAsync<SomeEvent, SomeResult>(
                It.Is<MessageContext<SomeEvent>>(m => m.Message.Equals(someEvent)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        await dispatch.ConnectOutAsync<SomeEvent, SomeResult>(pipe);
        await pipe.ConnectOutAsync<SomeEvent, SomeResult>(_fEndPipe.Object);
        var actualResult = await _mediator.SendAsync<SomeEvent, SomeResult>(someEvent);

        actualResult.Should().Be(expectedResult);
    }
}

public readonly record struct SomeEvent(Guid Id);

public readonly record struct SomeResult(Guid Id);