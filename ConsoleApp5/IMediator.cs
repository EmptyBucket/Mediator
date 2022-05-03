using ConsoleApp5.Bindings;
using ConsoleApp5.Pipes;

namespace ConsoleApp5;

public interface IMediator : ISender, IPublisher
{
    MediatorConfiguration Configuration { get; }
}

public class MediatorConfiguration
{
    private readonly IPipe _dispatchPipe;
    private readonly IPipeBinder _dispatchPipeBinder;

    public MediatorConfiguration(IPipe dispatchPipe, IPipeBinder dispatchPipeBinder)
    {
        _dispatchPipe = dispatchPipe;
        _dispatchPipeBinder = dispatchPipeBinder;
    }
    
    public 
}