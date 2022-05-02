using ConsoleApp5.Topologies;

namespace ConsoleApp5;

public interface IMediator : ISender, IPublisher, ITopologyRegistry, ITopologyProvider
{
}