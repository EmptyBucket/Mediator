using System.Collections.Concurrent;
using ConsoleApp5.Pipes;

namespace ConsoleApp5.Registries;

public class TransportRegistry : ITransportRegistry, ITransportProvider
{
    private readonly ServiceFactory _serviceFactory;
    private readonly ConcurrentDictionary<string, Type> _transportTypes = new();

    public TransportRegistry(ServiceFactory serviceFactory)
    {
        _serviceFactory = serviceFactory;
    }

    public void AddTransport<TPipe>(string name) where TPipe : IPipe
    {
        _transportTypes.TryAdd(name, typeof(TPipe));
    }

    public void RemoveTransport(string name)
    {
        _transportTypes.TryRemove(name, out _);
    }

    public IPipe GetTransport(string name)
    {
        return (IPipe)_serviceFactory(_transportTypes[name]);
    }
}