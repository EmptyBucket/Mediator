namespace FlexMediator.Pipes;

public interface IPipeBindings : IReadOnlyDictionary<Route, IReadOnlySet<PipeBind>>
{
}