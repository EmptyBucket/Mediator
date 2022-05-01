using ConsoleApp5.Pipes;

namespace ConsoleApp5.Registries;

internal class RabbitMqPipeRegistry : IPipeRegistry, IPipeProvider
{
    private readonly IPipeRegistry _pipeRegistry;
    private readonly IPipeProvider _pipeProvider;

    public RabbitMqPipeRegistry(IPipeRegistry pipeRegistry, IPipeProvider pipeProvider)
    {
        _pipeRegistry = pipeRegistry;
        _pipeProvider = pipeProvider;
    }

    public void AddPipe(IPipe pipe, string? routingKey = null)
    {
        _pipeRegistry.AddPipe(pipe, routingKey);
    }

    public void AddPipe<TMessage>(IPipe pipe, string? routingKey = null)
    {
        _pipeRegistry.AddPipe<TMessage>(pipe, routingKey);
    }

    public void RemovePipe(IPipe pipe, string? routingKey = null)
    {
        _pipeRegistry.RemovePipe(pipe, routingKey);
    }

    public void RemovePipe<TMessage>(IPipe pipe, string? routingKey = null)
    {
        _pipeRegistry.RemovePipe<TMessage>(pipe, routingKey);
    }

    public IReadOnlyCollection<IPipe> GetPipes<TMessage>(string? routingKey = null)
    {
        return _pipeProvider.GetPipes<TMessage>(routingKey);
    }
}

internal class PipeRegistry : IPipeRegistry, IPipeProvider
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<(Type?, string?), List<IPipe>> _pipes = new();

    public void AddPipe(IPipe pipe, string? routingKey = null)
    {
        var key = ((Type?)null, routingKey);
        _lock.EnterWriteLock();
        _pipes.TryAdd(key, new List<IPipe>());
        _pipes[key].Add(pipe);
        _lock.ExitWriteLock();
    }

    public void AddPipe<TMessage>(IPipe pipe, string? routingKey = null)
    {
        var key = (typeof(TMessage), routingKey);
        _lock.EnterWriteLock();
        _pipes.TryAdd(key, new List<IPipe>());
        _pipes[key].Add(pipe);
        _lock.ExitWriteLock();
    }

    public void RemovePipe(IPipe pipe, string? routingKey = null)
    {
        var key = ((Type?)null, routingKey);
        _lock.EnterWriteLock();

        if (_pipes.TryGetValue(key, out var list))
        {
            list.Remove(pipe);

            if (!list.Any()) _pipes.Remove(key);
        }

        _lock.ExitWriteLock();
    }

    public void RemovePipe<TMessage>(IPipe pipe, string? routingKey = null)
    {
        var key = (typeof(TMessage), routingKey);
        _lock.EnterWriteLock();

        if (_pipes.TryGetValue(key, out var list))
        {
            list.Remove(pipe);

            if (!list.Any()) _pipes.Remove(key);
        }

        _lock.ExitWriteLock();
    }

    public IReadOnlyCollection<IPipe> GetPipes<TMessage>(string? routingKey = null)
    {
        var key = (typeof(TMessage), routingKey);

        _lock.EnterReadLock();
        if (_pipes.TryGetValue(key, out var handlers))
            return handlers.ToArray();
        _lock.ExitReadLock();

        return Array.Empty<IPipe>();
    }
}