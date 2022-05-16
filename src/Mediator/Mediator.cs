using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.PublishSubscribe;
using Mediator.Pipes.RequestResponse;

namespace Mediator;

internal class Mediator : IMediator
{
    private readonly ConnectingPipe _pipe;

    public Mediator(IServiceProvider serviceProvider)
    {
        _pipe = new ConnectingPipe(serviceProvider);
    }

    public async Task PublishAsync<TMessage>(TMessage message,
        Func<MessageContext<TMessage>, MessageContext<TMessage>>? ctxBuilder = null, CancellationToken token = default)
    {
        var route = Route.For<TMessage>();
        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var ctx = new MessageContext<TMessage>(route, messageId, correlationId, DateTimeOffset.Now, message);
        ctxBuilder?.Invoke(ctx);
        await _pipe.PassAsync(ctx, token);
    }

    public async Task<TResult> SendAsync<TMessage, TResult>(TMessage message,
        Func<MessageContext<TMessage>, MessageContext<TMessage>>? ctxBuilder = null, CancellationToken token = default)
    {
        var route = Route.For<TMessage, TResult>();
        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var ctx = new MessageContext<TMessage>(route, messageId, correlationId, DateTimeOffset.Now, message);
        ctxBuilder?.Invoke(ctx);
        return await _pipe.PassAsync<TMessage, TResult>(ctx, token);
    }

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default) =>
        _pipe.ConnectOutAsync<TMessage>(pipe, routingKey, subscriptionId, token);

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default) =>
        _pipe.ConnectOutAsync<TMessage, TResult>(pipe, routingKey, token);

    public async ValueTask DisposeAsync() => await _pipe.DisposeAsync();
}