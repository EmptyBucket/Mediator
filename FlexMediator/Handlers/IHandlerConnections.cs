using FlexMediator.Utils;

namespace FlexMediator.Handlers;

public interface IHandlerConnections : IReadOnlyDictionary<Route, IReadOnlySet<HandlerConnection>>
{
}