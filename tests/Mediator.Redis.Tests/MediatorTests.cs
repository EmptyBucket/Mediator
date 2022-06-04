using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Mediator.Configurations;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Redis.Configurations;
using Mediator.Redis.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;

namespace Mediator.Redis.Tests
{
    public class MediatorTests
    {
        private const string RedisEndpoint = "localhost:6379";

        private ServiceProvider _serviceProvider;
        private IMediator _mediator;
        private Mock<IPipe> _endPipe;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(
                    new ConfigurationOptions { EndPoints = { RedisEndpoint }, AllowAdmin = true, DefaultDatabase = 1 }))
                .AddSingleton(p => p.GetRequiredService<IConnectionMultiplexer>().GetServer(RedisEndpoint))
                .AddMediator(b => b.BindRedis(), lifetime: ServiceLifetime.Transient);
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        [SetUp]
        public async Task Setup()
        {
            await FlushDatabaseAsync();

            _mediator = _serviceProvider.GetRequiredService<IMediator>();
            _endPipe = new Mock<IPipe>();
        }

        [TearDown]
        public async Task Teardown() => await _mediator.DisposeAsync();

        [OneTimeTearDown]
        public Task OneTimeTeardown() => FlushDatabaseAsync();

        private async Task FlushDatabaseAsync()
        {
            var server = _serviceProvider.GetRequiredService<IServer>();
            await server.FlushDatabaseAsync();
        }

        [Test]
        public async Task PublishAsync_WhenRabbitMqTopology_CallPassAsync()
        {
            var (dispatch, _, pipeFactory) = _mediator.Topology;
            await using var pipe = pipeFactory.Create<RedisMqPipe>();
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
        public async Task PublishAsync_WhenRabbitStreamTopology_CallPassAsync()
        {
            var (dispatch, _, pipeFactory) = _mediator.Topology;
            await using var pipe = pipeFactory.Create<RedisStreamPipe>();
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
            await using var pipe = pipeFactory.Create<RedisMqPipe>();
            var someEvent = new SomeEvent(Guid.NewGuid());

            await dispatch.ConnectOutAsync<SomeEvent, SomeResult>(pipe);
            await pipe.ConnectOutAsync<SomeEvent, SomeResult>(_endPipe.Object);
            await _mediator.SendAsync<SomeEvent, SomeResult>(someEvent);

            _endPipe.Verify(p => p.PassAsync<SomeEvent, SomeResult>(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()));
            await pipe.DisposeAsync();
        }

        [Test]
        public async Task SendAsync_WhenRabbitMqTopologyWithResult_ReturnResult()
        {
            var (dispatch, _, pipeFactory) = _mediator.Topology;
            await using var pipe = pipeFactory.Create<RedisMqPipe>();
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