using ConsoleApp5.Bindings;
using ConsoleApp5.Pipes;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp5.Transports;

public class MemoryTransportFactory : ITransportFactory
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public MemoryTransportFactory(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public Transport Create()
    {
        var bindings = new MemoryBindingRegistry();
        var pipe = new BindingPipe(bindings, _serviceScopeFactory);
        return new Transport("memory", pipe, bindings);
    }
}