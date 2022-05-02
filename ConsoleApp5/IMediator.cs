using ConsoleApp5.Registries;

namespace ConsoleApp5;

public interface IMediator : ISender, IPublisher, ITopologyRegistry, ITopologyProvider, ITransportRegistry, IPipeProvider
{
}