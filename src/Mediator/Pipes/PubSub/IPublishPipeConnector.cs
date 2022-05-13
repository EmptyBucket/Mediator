namespace Mediator.Pipes.PubSub;

public interface IPublishPipeConnector : IAsyncDisposable
{
    Task<PublishPipeConnection> ConnectOutAsync<TMessage>(IPublishPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default);
}