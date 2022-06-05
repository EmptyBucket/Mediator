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
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace Mediator.RabbitMq.Tests
{
    public class MediatorTests
    {
        private const string RabbitMqEndpoint = "localhost";
        private const string VhostName = "test";

        private ServiceProvider _serviceProvider;
        private IMediator _mediator;
        private Mock<IPipe> _endPipe;

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
            _endPipe = new Mock<IPipe>();
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
            await pipe.ConnectOutAsync<SomeEvent>(_endPipe.Object);
            await _mediator.PublishAsync(someEvent);
            // for an asynchronous PassAsync which is on a different thread
            await Task.Delay(50);

            _endPipe.Verify(p => p.PassAsync(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task SendAsync_WhenRabbitMqTopologyWithResult_CallPassAsync()
        {
            var (dispatch, _, pipeFactory) = _mediator.Topology;
            await using var pipe = pipeFactory.Create<RabbitMqPipe>();
            var someEvent = new SomeEvent(Guid.NewGuid());

            await dispatch.ConnectOutAsync<SomeEvent, SomeResult>(pipe);
            await pipe.ConnectOutAsync<SomeEvent, SomeResult>(_endPipe.Object);
            await _mediator.SendAsync<SomeEvent, SomeResult>(someEvent);

            _endPipe.Verify(p => p.PassAsync<SomeEvent, SomeResult>(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task SendAsync_WhenRabbitMqTopologyWithResult_ReturnResult()
        {
            var (dispatch, _, pipeFactory) = _mediator.Topology;
            await using var pipe = pipeFactory.Create<RabbitMqPipe>();
            var someEvent = new SomeEvent(Guid.NewGuid());
            var expectedResult = new SomeResult(Guid.NewGuid());
            _endPipe.Setup(p => p.PassAsync<SomeEvent, SomeResult>(
                    It.Is<MessageContext<SomeEvent>>(m => m.Message.Equals(someEvent)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            await dispatch.ConnectOutAsync<SomeEvent, SomeResult>(pipe);
            await pipe.ConnectOutAsync<SomeEvent, SomeResult>(_endPipe.Object);
            var actualResult = await _mediator.SendAsync<SomeEvent, SomeResult>(someEvent);

            actualResult.Should().Be(expectedResult);
        }
    }

    public readonly record struct SomeEvent(Guid Id);

    public readonly record struct SomeResult(Guid Id);
}