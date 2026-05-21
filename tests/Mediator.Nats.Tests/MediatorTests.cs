using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator.Configurations;
using Mediator.Handlers;
using Mediator.Nats.Pipes;
using Mediator.Pipes;
using Mediator.Tests;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NATS.Client.Core;
using NATS.Net;
using NUnit.Framework;

namespace Mediator.Nats.Tests;

public class NatsStreamPipeTests
{
    private const string NatsEndpoint = "nats://localhost:4222";

    private ServiceProvider _serviceProvider;
    private INatsConnection _natsConnection;
    private IMediator _mediator;
    private Mock<IPipe> _fEndPipe;
    private Mock<IPipe> _sEndPipe;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var serviceCollection = new ServiceCollection();
        _natsConnection = new NatsConnection(new NatsOpts { Url = NatsEndpoint });
        await _natsConnection.ConnectAsync();
        serviceCollection
            .AddSingleton(_natsConnection)
            .AddSingleton<NatsStreamPipe>()
            .AddMediator(lifetime: ServiceLifetime.Transient);
        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    [SetUp]
    public void Setup()
    {
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
        _fEndPipe = new Mock<IPipe>();
        _sEndPipe = new Mock<IPipe>();
    }

    [TearDown]
    public async Task Teardown()
    {
        await _mediator.DisposeAsync();
        await CleanupStreamsAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTeardown()
    {
        await CleanupStreamsAsync();
        await _natsConnection.DisposeAsync();
    }

    private async Task CleanupStreamsAsync()
    {
        var jetStream = _natsConnection.CreateJetStreamContext();

        await foreach (var stream in jetStream.ListStreamsAsync())
            if (stream.Info.Config.Name?.Contains("SomeEvent") ?? false)
                await jetStream.DeleteStreamAsync(stream.Info.Config.Name);
    }

    [Test]
    public async Task PublishAsync_WhenNatsStreamTopology_CallPassAsync()
    {
        var (dispatch, _) = _mediator.Topology;
        var pipe = _serviceProvider.GetRequiredService<NatsStreamPipe>();
        var someEvent = new SomeEvent(Guid.NewGuid());

        await dispatch.ConnectOutAsync<SomeEvent>(pipe);
        await pipe.ConnectOutAsync<SomeEvent>(_fEndPipe.Object);
        await _mediator.PublishAsync(someEvent);
        await Task.Delay(500);

        await _fEndPipe.VerifyAsync(p => p.PassAsync(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Test]
    public async Task PublishAsync_WhenNatsStreamTopologyWithTwoConsumers_CallPassAsyncOnBoth()
    {
        var (dispatch, _) = _mediator.Topology;
        var pipe = _serviceProvider.GetRequiredService<NatsStreamPipe>();
        var someEvent = new SomeEvent(Guid.NewGuid());

        await dispatch.ConnectOutAsync<SomeEvent>(pipe);
        await pipe.ConnectOutAsync<SomeEvent>(_fEndPipe.Object, subscriptionId: "consumer1");
        await pipe.ConnectOutAsync<SomeEvent>(_sEndPipe.Object, subscriptionId: "consumer2");
        await _mediator.PublishAsync(someEvent);
        await Task.Delay(500);

        await _fEndPipe.VerifyAsync(p => p.PassAsync(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()),
            Times.Once());
        await _sEndPipe.VerifyAsync(p => p.PassAsync(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Test]
    public async Task PublishAsync_WhenNatsStreamTopologyWithDifferentEvents_ProcessCorrectly()
    {
        var (dispatch, _) = _mediator.Topology;
        var pipe = _serviceProvider.GetRequiredService<NatsStreamPipe>();
        var someEvent1 = new SomeEvent(Guid.NewGuid());
        var someEvent2 = new SomeEvent(Guid.NewGuid());

        await dispatch.ConnectOutAsync<SomeEvent>(pipe);
        await pipe.ConnectOutAsync<SomeEvent>(_fEndPipe.Object);

        await _mediator.PublishAsync(someEvent1);
        await _mediator.PublishAsync(someEvent2);

        await Task.Delay(500);

        await _fEndPipe.VerifyAsync(p => p.PassAsync(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent1)), It.IsAny<CancellationToken>()),
            Times.Once());
        await _fEndPipe.VerifyAsync(p => p.PassAsync(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent2)), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    public readonly record struct SomeEvent(Guid Id);
}