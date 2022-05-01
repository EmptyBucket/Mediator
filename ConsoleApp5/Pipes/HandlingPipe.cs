using System.Data;
using ConsoleApp5.Registries;

namespace ConsoleApp5.Pipes;

public class HandlingPipe : IPipe
{
    private readonly IHandlerProvider _handlerProvider;

    public HandlingPipe(IHandlerProvider handlerProvider)
    {
        _handlerProvider = handlerProvider;
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options, CancellationToken token)
    {
        var handlers = _handlerProvider.GetHandlers<TMessage>(options.RoutingKey);
        await Task.WhenAll(handlers.Cast<IHandler<TMessage>>().Select(h => h.Handle(message, options, token)));
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token)
    {
        var handlers = _handlerProvider.GetHandlers<TMessage>(options.RoutingKey);

        if (handlers.Count != 1) throw new InvalidConstraintException("Must be single handler");

        return await handlers.Cast<IHandler<TMessage, TResult>>().Single().Handle(message, options, token);
    }
}