using ConsoleApp5.HandlerBindings;
using ConsoleApp5.Models;

namespace ConsoleApp5;

public interface IMediator : ISender, IPublisher
{
    MediatorConfiguration Configuration { get; }
}

public class MediatorConfiguration
{
    public MediatorConfiguration(IHandlerBinder handlerBinder, )
    {
    }
}

public class TopologyBinder
{
    public Task Bind<TMessage>(string transportName, string routingKey = "")
    {
        
    }
}