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

using Mediator.Pipes;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator;

internal class MediatorFactory : IMediatorFactory
{
    private readonly Lazy<Task<IMediator>> _mediator;

    public MediatorFactory(IServiceProvider serviceProvider,
        Func<IServiceProvider, MediatorTopology, Task> connectPipes)
    {
        _mediator = new Lazy<Task<IMediator>>(async () =>
        {
            var pipeFactory = serviceProvider.GetRequiredService<IPipeFactory>();
            var dispatchPipe = pipeFactory.Create<Pipe>();
            var receivePipe = pipeFactory.Create<Pipe>();
            var mediatorTopology = new MediatorTopology(dispatchPipe, receivePipe);
            await connectPipes(serviceProvider, mediatorTopology);
            var mediator = new Mediator(mediatorTopology);
            return mediator;
        });
    }

    public async Task<IMediator> CreateAsync() => await _mediator.Value;
}