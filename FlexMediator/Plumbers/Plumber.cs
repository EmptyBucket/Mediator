using FlexMediator.Pipes;

namespace FlexMediator.Plumbers;

public class Plumber : IPlumber
{
    private readonly IPipeConnector _pipeConnector;
    private readonly IPipeFactory _pipeFactory;
    private readonly List<PipeConnection> _pipeConnections = new();

    public Plumber(IPipeConnector pipeConnector, IPipeFactory pipeFactory)
    {
        _pipeConnector = pipeConnector;
        _pipeFactory = pipeFactory;
    }

    public async Task<IPlumber> Connect<TMessage, TPipe>(string routingKey = "", Action<IPipeConnector>? make = null)
        where TPipe : IPipe, IPipeConnector
    {
        var nextPipe = _pipeFactory.Create<TPipe>();
        make?.Invoke(nextPipe);

        var pipeConnection = await _pipeConnector.Connect<TMessage>(nextPipe, routingKey);
        _pipeConnections.Add(pipeConnection);

        return this;
    }

    public async Task<IPlumber> Connect<TMessage, TResult, TPipe>(string routingKey = "",
        Action<IPipeConnector>? make = null)
        where TPipe : IPipe, IPipeConnector
    {
        var nextPipe = _pipeFactory.Create<TPipe>();
        make?.Invoke(nextPipe);

        var pipeConnection = await _pipeConnector.Connect<TMessage, TResult>(nextPipe, routingKey);
        _pipeConnections.Add(pipeConnection);

        return this;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections) await pipeConnection.DisposeAsync();
    }
}