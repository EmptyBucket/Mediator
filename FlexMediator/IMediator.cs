using FlexMediator.Pipes;

namespace FlexMediator;

public interface IMediator : ISender, IPublisher
{
    IPipeConnector PipeConnector { get; }
}