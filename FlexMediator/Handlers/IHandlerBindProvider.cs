namespace FlexMediator.Handlers;

public interface IHandlerBindProvider : IReadOnlyDictionary<Route, IReadOnlySet<HandlerBind>>
{
}