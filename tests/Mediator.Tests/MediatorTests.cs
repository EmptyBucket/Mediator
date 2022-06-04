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
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace Mediator.Tests
{
    public class MediatorTests
    {
        private ServiceProvider _serviceProvider;
        private IMediator _mediator;
        private Mock<IPipe> _endPipe;

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
            _endPipe = new Mock<IPipe>();
        }

        [TearDown]
        public async Task Teardown() => await _mediator.DisposeAsync();

        [Test]
        public async Task PublishAsync_WhenDirectTopology_CallPassAsync()
        {
            var (dispatch, _) = _mediator.Topology;
            var someEvent = new SomeEvent(Guid.NewGuid());

            await dispatch.ConnectOutAsync<SomeEvent>(_endPipe.Object);
            await _mediator.PublishAsync(someEvent);

            _endPipe.Verify(p => p.PassAsync(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task PublishAsync_WhenReceiveTopology_CallPassAsync()
        {
            var (dispatch, receive) = _mediator.Topology;
            var someEvent = new SomeEvent(Guid.NewGuid());

            await dispatch.ConnectOutAsync<SomeEvent>(receive);
            await receive.ConnectOutAsync<SomeEvent>(_endPipe.Object);
            await _mediator.PublishAsync(someEvent);

            _endPipe.Verify(p => p.PassAsync(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task SendAsync_WhenDirectTopologyWithResult_CallPassAsync()
        {
            var (dispatch, _) = _mediator.Topology;
            var someEvent = new SomeEvent(Guid.NewGuid());

            await dispatch.ConnectOutAsync<SomeEvent, SomeResult>(_endPipe.Object);
            await _mediator.SendAsync<SomeEvent, SomeResult>(someEvent);

            _endPipe.Verify(p => p.PassAsync<SomeEvent, SomeResult>(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task SendAsync_WhenDirectTopologyWithResult_ReturnResult()
        {
            var (dispatch, _) = _mediator.Topology;
            var someEvent = new SomeEvent(Guid.NewGuid());
            var expectedResult = new SomeResult(Guid.NewGuid());
            _endPipe.Setup(p => p.PassAsync<SomeEvent, SomeResult>(It.IsAny<MessageContext<SomeEvent>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            await dispatch.ConnectOutAsync<SomeEvent, SomeResult>(_endPipe.Object);
            var actualResult = await _mediator.SendAsync<SomeEvent, SomeResult>(someEvent);

            actualResult.Should().Be(expectedResult);
        }

        [Test]
        public async Task SendAsync_WhenReceiveTopologyWithResult_CallPassAsync()
        {
            var (dispatch, receive) = _mediator.Topology;
            var someEvent = new SomeEvent(Guid.NewGuid());

            await dispatch.ConnectOutAsync<SomeEvent, SomeResult>(receive);
            await receive.ConnectOutAsync<SomeEvent, SomeResult>(_endPipe.Object);
            await _mediator.SendAsync<SomeEvent, SomeResult>(someEvent);

            _endPipe.Verify(p => p.PassAsync<SomeEvent, SomeResult>(
                It.Is<MessageContext<SomeEvent>>(c => c.Message.Equals(someEvent)), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task SendAsync_WhenReceiveTopologyWithResult_ReturnResult()
        {
            var (dispatch, receive) = _mediator.Topology;
            var someEvent = new SomeEvent(Guid.NewGuid());
            var expectedResult = new SomeResult(Guid.NewGuid());
            _endPipe.Setup(p => p.PassAsync<SomeEvent, SomeResult>(
                    It.Is<MessageContext<SomeEvent>>(m => m.Message.Equals(someEvent)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            await dispatch.ConnectOutAsync<SomeEvent, SomeResult>(receive);
            await receive.ConnectOutAsync<SomeEvent, SomeResult>(_endPipe.Object);
            var actualResult = await _mediator.SendAsync<SomeEvent, SomeResult>(someEvent);

            actualResult.Should().Be(expectedResult);
        }

        public readonly record struct SomeEvent(Guid Id);

        public readonly record struct SomeResult(Guid Id);
    }
}