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

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Mediator.Configurations;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Redis.Configurations;
using Mediator.Redis.Pipes;
using Mediator.Tests;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;

namespace Mediator.Redis.Tests;

public class MediatorTests
{
    private const string RedisEndpoint = "localhost:6379";

    private ServiceProvider _serviceProvider;
    private IMediator _mediator;
    private Mock<IPipe> _fEndPipe;
    private Mock<IPipe> _sEndPipe;

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
        _fEndPipe = new Mock<IPipe>();
        _sEndPipe = new Mock<IPipe>();
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
    public async Task PublishAsync_WhenRedisMqTopology_CallPassAsync()
    {
        var (dispatch, _, pipeFactory) = _mediator.Topology;
        await using var pipe = pipeFactory.Create<RedisMqPipe>();
        var someEvent = new SomeEvent(Guid.NewGuid());

        await dispatch.ConnectOutAsync<SomeEvent>(pipe);
        await pipe.ConnectOutAsync<SomeEvent>(_fEndPipe.Object);
        await _mediator.PublishAsync(someEvent);

        await _fEndPipe.VerifyAsync(p => p.PassAsync(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()),
            Times.Once());
    }
    
    [Test]
    public async Task PublishAsync_WhenRedisMqTopologyWithTwoConsumers_CallPassAsyncOnBoth()
    {
        var (dispatch, _, pipeFactory) = _mediator.Topology;
        await using var pipe = pipeFactory.Create<RedisMqPipe>();
        var someEvent = new SomeEvent(Guid.NewGuid());

        await dispatch.ConnectOutAsync<SomeEvent>(pipe);
        await pipe.ConnectOutAsync<SomeEvent>(_fEndPipe.Object);
        await pipe.ConnectOutAsync<SomeEvent>(_sEndPipe.Object);
        await _mediator.PublishAsync(someEvent);

        await _fEndPipe.VerifyAsync(p => p.PassAsync(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()),
            Times.Once());
        await _sEndPipe.VerifyAsync(p => p.PassAsync(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Test]
    public async Task PublishAsync_WhenRedisStreamTopology_CallPassAsync()
    {
        var (dispatch, _, pipeFactory) = _mediator.Topology;
        await using var pipe = pipeFactory.Create<RedisStreamPipe>();
        var someEvent = new SomeEvent(Guid.NewGuid());

        await dispatch.ConnectOutAsync<SomeEvent>(pipe);
        await pipe.ConnectOutAsync<SomeEvent>(_fEndPipe.Object);
        await _mediator.PublishAsync(someEvent);

        await _fEndPipe.VerifyAsync(p => p.PassAsync(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()),
            Times.Once());
    }
    
    [Test]
    public async Task PublishAsync_WhenRedisStreamTopologyWithTwoConsumers_CallPassAsyncOnBoth()
    {
        var (dispatch, _, pipeFactory) = _mediator.Topology;
        await using var pipe = pipeFactory.Create<RedisStreamPipe>();
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
    public async Task PublishAsyncTwice_WhenRedisStreamTopologyWithTwoConsumers_CallPassAsyncOnBoth()
    {
        var (dispatch, _, pipeFactory) = _mediator.Topology;
        await using var pipe = pipeFactory.Create<RedisStreamPipe>();
        var someEvent = new SomeEvent(Guid.NewGuid());
        async Task DoSomeWork() => await Task.Delay(1_000);
        _fEndPipe.Setup(p => p.PassAsync(It.IsAny<MessageContext<SomeEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(DoSomeWork);
        _sEndPipe.Setup(p => p.PassAsync(It.IsAny<MessageContext<SomeEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(DoSomeWork);

        await dispatch.ConnectOutAsync<SomeEvent>(pipe);
        await pipe.ConnectOutAsync<SomeEvent>(_fEndPipe.Object, subscriptionId: ":1");
        await pipe.ConnectOutAsync<SomeEvent>(_sEndPipe.Object, subscriptionId: ":2");
        await _mediator.PublishAsync(someEvent);
        // batching workaround
        await Task.Delay(101);
        await _mediator.PublishAsync(someEvent);

        await _fEndPipe.VerifyAsync(p => p.PassAsync(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()),
            Times.Once());
        await _sEndPipe.VerifyAsync(p => p.PassAsync(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Test]
    public async Task SendAsync_WhenRedisMqTopologyWithResult_CallPassAsync()
    {
        var (dispatch, _, pipeFactory) = _mediator.Topology;
        await using var pipe = pipeFactory.Create<RedisMqPipe>();
        var someEvent = new SomeEvent(Guid.NewGuid());

        await dispatch.ConnectOutAsync<SomeEvent, SomeResult>(pipe);
        await pipe.ConnectOutAsync<SomeEvent, SomeResult>(_fEndPipe.Object);
        await _mediator.SendAsync<SomeEvent, SomeResult>(someEvent);

        _fEndPipe.Verify(p => p.PassAsync<SomeEvent, SomeResult>(
            It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()));
    }

    [Test]
    public async Task SendAsync_WhenRedisMqTopologyWithResult_ReturnResult()
    {
        var (dispatch, _, pipeFactory) = _mediator.Topology;
        await using var pipe = pipeFactory.Create<RedisMqPipe>();
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