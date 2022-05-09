using System.Collections.Immutable;
using Mediator.Pipes;

namespace Mediator.Configurations;

internal class PipeBinder : IPipeBinder
{
    private IImmutableDictionary<PipeBind, Type> _pipeBinds = ImmutableDictionary<PipeBind, Type>.Empty;

    public IPipeBinder BindPipe(Type pipeType, string pipeName = "")
    {
        _pipeBinds = _pipeBinds.SetItem(new PipeBind(pipeType, pipeName), pipeType);

        foreach (var @interface in pipeType.GetInterfaces().Where(i => i.IsAssignableTo(typeof(IPipe))))
            _pipeBinds = _pipeBinds.SetItem(new PipeBind(@interface, pipeName), pipeType);

        return this;
    }

    public IReadOnlyDictionary<PipeBind, Type> Build() => _pipeBinds;
}