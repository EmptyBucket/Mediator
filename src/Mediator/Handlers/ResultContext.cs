using Mediator.Pipes.Utils;

namespace Mediator.Handlers;

public record ResultContext<TResult>(Route Route, string MessageId, string CorrelationId)
{
    public TResult? Result { get; set; }

    public Exception? Exception { get; set; }
};