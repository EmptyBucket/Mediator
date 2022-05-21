namespace Mediator.Pipes;

public interface IConnectingPipe : IPipe, IPipeConnector, IConnectingPubPipe, IConnectingReqPipe
{
}