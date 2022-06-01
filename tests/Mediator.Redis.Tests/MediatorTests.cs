using System;
using System.Threading;
using System.Threading.Tasks;
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
        private ServiceProvider _serviceProvider;
        private IMediator _mediator;
        private Mock<IPipe> _pipe;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(new ConfigurationOptions
                {
                    EndPoints = { "localhost:6379" }, AllowAdmin = true, DefaultDatabase = 1
                }))
                .AddMediator(b => b.BindRedis(), lifetime: ServiceLifetime.Transient);
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        [OneTimeTearDown]
        public async Task Teardown()
        {
            var connectionMultiplexer = _serviceProvider.GetRequiredService<IConnectionMultiplexer>();
            var server = connectionMultiplexer.GetServer("localhost:6379");
            await server.FlushDatabaseAsync(1);
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
            var pipe = pipeFactory.Create<RedisMqPipe>();
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
        public async Task PublishAsync_WhenRabbitStreamTopology_CallPassAsync()
        {
            var (dispatch, _, pipeFactory) = _mediator.Topology;
            var pipe = pipeFactory.Create<RedisStreamPipe>();
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
            var pipe = pipeFactory.Create<RedisMqPipe>();
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