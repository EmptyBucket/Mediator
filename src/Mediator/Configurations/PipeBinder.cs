namespace Mediator.Configurations;

internal class PipeBinder : IPipeBinder
{
    private readonly Dictionary<PipeBind, Type> _pipeBinds = new();

    public IPipeBinder Bind(Type pipeType, Type pipeImplType, string pipeName = "")
    {
        _pipeBinds[new PipeBind(pipeType, pipeName)] = pipeImplType;

        return this;
    }

    public IReadOnlyDictionary<PipeBind, Type> Build() => _pipeBinds;
}