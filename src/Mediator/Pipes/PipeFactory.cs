using Mediator.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator;

internal class PipeFactory : IPipeFactory
{
    private readonly IReadOnlyDictionary<PipeBind, Type> _pipeBinds;
    private readonly IServiceProvider _serviceProvider;

    public PipeFactory(IReadOnlyDictionary<PipeBind, Type> pipeBinds, IServiceProvider serviceProvider)
    {
        _pipeBinds = pipeBinds;
        _serviceProvider = serviceProvider;
    }

    public TPipe Create<TPipe>(string pipeName = "")
    {
        var pipeType = typeof(TPipe);

        if (_pipeBinds.TryGetValue(new PipeBind(pipeType, pipeName), out var type))
            return (TPipe)ActivatorUtilities.CreateInstance(_serviceProvider, type);

        if (pipeType.IsGenericType &&
            _pipeBinds.TryGetValue(new PipeBind(pipeType.GetGenericTypeDefinition(), pipeName), out var gType))
        {
            gType = gType.MakeGenericType(pipeType.GetGenericArguments());
            return (TPipe)ActivatorUtilities.CreateInstance(_serviceProvider, gType);
        }

        throw new InvalidOperationException($"{typeof(TPipe)} was not bounded");
    }
}