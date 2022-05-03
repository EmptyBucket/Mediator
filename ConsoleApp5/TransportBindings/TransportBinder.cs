using ConsoleApp5.Models;

namespace ConsoleApp5.TransportBindings;

internal class TransportBinder : ITransportBinder, ITransportBindProvider
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<Route, HashSet<TransportBind>> _transportBindings = new();

    public Task Bind<TMessage>(Transport transport, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            _transportBindings.TryAdd(route, new HashSet<TransportBind>());
            _transportBindings[route].Add(new TransportBind(route, transport));
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task Unbind<TMessage>(Transport transport, string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        _lock.EnterWriteLock();

        try
        {
            if (_transportBindings.TryGetValue(route, out var set))
            {
                set.Remove(new TransportBind(route, transport));

                if (!set.Any()) _transportBindings.Remove(route);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public IEnumerable<TransportBind> GetBinds<TMessage>(string routingKey = "")
    {
        var route = new Route(typeof(TMessage), routingKey);

        return _transportBindings.TryGetValue(route, out var transportBinds)
            ? transportBinds
            : Enumerable.Empty<TransportBind>();
    }
}