using Mediator.Pipes.PublishSubscribe;
using Mediator.Pipes.RequestResponse;

namespace Mediator.Pipes;

public interface IPipeConnector : IPubPipeConnector, IReqPipeConnector
{
}