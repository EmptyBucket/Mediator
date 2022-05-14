using Mediator.Pipes.PublishSubscribe;
using Mediator.Pipes.RequestResponse;

namespace Mediator.Pipes;

public interface IPipe : IPubPipe, IReqPipe
{
}