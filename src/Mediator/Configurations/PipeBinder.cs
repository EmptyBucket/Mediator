using System.Collections.Immutable;

namespace Mediator.Configurations;

internal class PipeBinder : IPipeBinder
{
    private IImmutableDictionary<PipeBind, Type> _pipeBinds = ImmutableDictionary<PipeBind, Type>.Empty;

    public IPipeBinder Bind(Type pipeType, Type pipeImplType, string pipeName = "")
    {
        _pipeBinds = _pipeBinds.SetItem(new PipeBind(pipeType, pipeName), pipeImplType);

        return this;
    }

    public IReadOnlyDictionary<PipeBind, Type> Build() => _pipeBinds;
}