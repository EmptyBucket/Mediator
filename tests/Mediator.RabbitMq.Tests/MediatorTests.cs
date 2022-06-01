using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Management.Client;
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
        private ServiceProvider _serviceProvider;
        private IMediator _mediator;
        private Mock<IPipe> _pipe;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddSingleton<IManagementClient>(new ManagementClient("localhost", "guest", "guest"))
                .RegisterEasyNetQ("host=localhost;virtualHost=test")
                .AddMediator(b => b.BindRabbitMq(), lifetime: ServiceLifetime.Transient);
            _serviceProvider = serviceCollection.BuildServiceProvider();
            var managementClient = _serviceProvider.GetRequiredService<IManagementClient>();
            await managementClient.CreateVhostAsync("test");
        }

        [OneTimeTearDown]
        public async Task Teardown()
        {
            var managementClient = _serviceProvider.GetRequiredService<IManagementClient>();
            var vhost = await managementClient.GetVhostAsync("test");
            await managementClient.DeleteVhostAsync(vhost);
        }

        [SetUp]
        public void Setup()
        {
            _mediator = _serviceProvider.GetRequiredService<IMediator>();
            _pipe = new Mock<IPipe>();
        }
        
        [Test]
        public async Task PublishAsync_WhenRabbitMqTopology_CallPassAsync()
        {
            var (dispatch, _, pipeFactory) = _mediator.Topology;
            var pipe = pipeFactory.Create<RabbitMqPipe>();
            var someEvent = new SomeEvent(Guid.NewGuid());

            await dispatch.ConnectOutAsync<SomeEvent>(pipe);
            await pipe.ConnectOutAsync<SomeEvent>(_pipe.Object);
            await _mediator.PublishAsync(someEvent);
            // for an asynchronous PassAsync which is on a different thread
            await Task.Delay(50);

            _pipe.Verify(p => p.PassAsync(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task SendAsync_WhenRabbitMqTopologyWithResult_CallPassAsync()
        {
            var (dispatch, _, pipeFactory) = _mediator.Topology;
            var pipe = pipeFactory.Create<RabbitMqPipe>();
            var someEvent = new SomeEvent(Guid.NewGuid());

            await dispatch.ConnectOutAsync<SomeEvent, SomeResult>(pipe);
            await pipe.ConnectOutAsync<SomeEvent, SomeResult>(_pipe.Object);
            await _mediator.SendAsync<SomeEvent, SomeResult>(someEvent);

            _pipe.Verify(p => p.PassAsync<SomeEvent, SomeResult>(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()));
        }
    }

    public readonly record struct SomeEvent(Guid Id);

    public readonly record struct SomeResult(Guid Id);
}