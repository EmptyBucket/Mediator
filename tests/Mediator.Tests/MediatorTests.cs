using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator.Configurations;
using Mediator.Handlers;
using Mediator.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace Mediator.Tests
{
    public class MediatorTests
    {
        private ServiceProvider _serviceProvider;
        private IMediator _mediator;
        private Mock<IPipe> _pipe;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddMediator(lifetime: ServiceLifetime.Transient);
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        [SetUp]
        public void Setup()
        {
            _mediator = _serviceProvider.GetRequiredService<IMediator>();
            _pipe = new Mock<IPipe>();
        }

        [Test]
        public async Task PublishAsync_WhenDirectTopology_CallPassAsync()
        {
            var (dispatch, _) = _mediator.Topology;
            var someEvent = new SomeEvent(Guid.NewGuid());

            await dispatch.ConnectOutAsync<SomeEvent>(_pipe.Object);
            await _mediator.PublishAsync(someEvent);

            _pipe.Verify(p => p.PassAsync(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task PublishAsync_WhenReceiveTopology_CallPassAsync()
        {
            var (dispatch, receive) = _mediator.Topology;
            var someEvent = new SomeEvent(Guid.NewGuid());

            await dispatch.ConnectOutAsync<SomeEvent>(receive);
            await receive.ConnectOutAsync<SomeEvent>(_pipe.Object);
            await _mediator.PublishAsync(someEvent);

            _pipe.Verify(p => p.PassAsync(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task SendAsync_WhenDirectTopologyWithResult_CallPassAsync()
        {
            var (dispatch, _) = _mediator.Topology;
            var someEvent = new SomeEvent(Guid.NewGuid());

            await dispatch.ConnectOutAsync<SomeEvent, SomeResult>(_pipe.Object);
            await _mediator.SendAsync<SomeEvent, SomeResult>(someEvent);

            _pipe.Verify(p => p.PassAsync<SomeEvent, SomeResult>(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task SendAsync_WhenReceiveTopologyWithResult_CallPassAsync()
        {
            var (dispatch, receive) = _mediator.Topology;
            var someEvent = new SomeEvent(Guid.NewGuid());

            await dispatch.ConnectOutAsync<SomeEvent, SomeResult>(receive);
            await receive.ConnectOutAsync<SomeEvent, SomeResult>(_pipe.Object);
            await _mediator.SendAsync<SomeEvent, SomeResult>(someEvent);

            _pipe.Verify(p => p.PassAsync<SomeEvent, SomeResult>(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()));
        }

        public readonly record struct SomeEvent(Guid Id);

        public readonly record struct SomeResult(Guid Id);
    }
}