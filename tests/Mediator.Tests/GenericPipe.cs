using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator.Handlers;
using Mediator.Pipes;

namespace Mediator.Tests;

internal class GenericPipe<T> : IPipe
{
    public Task PassAsync<TMessage>(MessageContext<TMessage> ctx, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> ctx,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}