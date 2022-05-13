using Mediator.Pipes.PubSub;
using Mediator.Pipes.RequestResponse;

namespace Mediator.Pipes;

public static class Route
{
    public static PublishRoute For<TMessage>(string routingKey = "") => new(typeof(TMessage), routingKey);

    public static SendRoute For<TMessage, TResult>(string routingKey = "") =>
        new(typeof(TMessage), typeof(TResult), routingKey);
}