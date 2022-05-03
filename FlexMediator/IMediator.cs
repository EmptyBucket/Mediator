using FlexMediator.Topologies;

namespace FlexMediator;

public interface IMediator : ISender, IPublisher
{
    MediatorConfiguration Configuration { get; }
}

public class MediatorConfiguration
{
    public MediatorConfiguration(IReadOnlyDictionary<string, Topology> topologies)
    {
        Topologies = topologies;
    }

    public IReadOnlyDictionary<string, Topology> Topologies { get; }
}