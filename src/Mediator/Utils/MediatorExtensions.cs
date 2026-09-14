namespace Mediator.Utils;

public static class MediatorExtensions
{
    /// <summary>
    /// Send message according publish/subscribe messaging model
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="message"></param>
    /// <param name="routingKey"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <returns></returns>
    public static Task PublishAsync<TMessage>(this IMediator mediator, TMessage message, string routingKey,
        CancellationToken cancellationToken = default) =>
        mediator.PublishAsync(message, new Options { RoutingKey = routingKey }, cancellationToken);

    /// <summary>
    /// Send message according request/response messaging model
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="message"></param>
    /// <param name="routingKey"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static Task<TResult> SendAsync<TMessage, TResult>(this IMediator mediator, TMessage message,
        string routingKey, CancellationToken cancellationToken = default) =>
        mediator.SendAsync<TMessage, TResult>(message, new Options { RoutingKey = routingKey }, cancellationToken);
}