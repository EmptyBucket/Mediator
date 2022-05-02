namespace ConsoleApp5;

public interface IMediator : ISender, IPublisher
{
    MediatorConfiguration Configuration { get; }
}

public class MediatorConfiguration
{
    
}