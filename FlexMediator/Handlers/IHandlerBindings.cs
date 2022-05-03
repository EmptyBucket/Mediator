namespace FlexMediator.Handlers;

public interface IHandlerBindings : IReadOnlyDictionary<Route, IReadOnlySet<HandlerBind>>
{
}