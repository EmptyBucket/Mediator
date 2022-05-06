using FlexMediator.Utils;

namespace FlexMediator.Pipes;

public interface IPipeConnections : IReadOnlyDictionary<Route, IReadOnlySet<PipeConnection>>
{
}