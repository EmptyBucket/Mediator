using FlexMediator.Handlers;
using FlexMediator.Pipes;

namespace FlexMediator.Plumbers;

public class Plumber : IPlumber
{
    private readonly IPipeConnector _pipeConnector;
    private readonly IPipeFactory _pipeFactory;

    public Plumber(IPipeConnector pipeConnector, IPipeFactory pipeFactory)
    {
        _pipeConnector = pipeConnector;
        _pipeFactory = pipeFactory;
    }

    public async Task<IPlumber> Connect<TMessage, TPipe>(string routingKey = "",
        Action<IPlumber<TMessage>>? next = null)
        where TPipe : IPipe, IPipeConnector
    {
        var nextPipe = _pipeFactory.Create<TPipe>();
        var nextPlumber = new Plumber<TMessage>(nextPipe, _pipeFactory);
        next?.Invoke(nextPlumber);

        await _pipeConnector.Connect<TMessage>(nextPipe, routingKey);
        return this;
    }

    public async Task<IPlumber> Connect<TMessage, TResult, TPipe>(string routingKey = "",
        Action<IPlumber<TMessage, TResult>>? next = null)
        where TPipe : IPipe, IPipeConnector
    {
        var nextPipe = _pipeFactory.Create<TPipe>();
        var nextPlumber = new Plumber<TMessage, TResult>(nextPipe, _pipeFactory);
        next?.Invoke(nextPlumber);

        await _pipeConnector.Connect<TMessage, TResult>(nextPipe, routingKey);
        return this;
    }

    public async Task<IPlumber> ConnectHandlers<TMessage>(string routingKey = "",
        Action<IHandlerPlumber<TMessage>>? next = null)
    {
        var nextPipe = _pipeFactory.Create<HandlingPipe>();
        var nextPlumber = new HandlerPlumber<TMessage>(nextPipe);
        next?.Invoke(nextPlumber);

        await _pipeConnector.Connect<TMessage>(nextPipe, routingKey);
        return this;
    }

    public async Task<IPlumber> ConnectHandlers<TMessage, TResult>(string routingKey = "",
        Action<IHandlerPlumber<TMessage, TResult>>? next = null)
    {
        var nextPipe = _pipeFactory.Create<HandlingPipe>();
        var nextPlumber = new HandlerPlumber<TMessage, TResult>(nextPipe);
        next?.Invoke(nextPlumber);

        await _pipeConnector.Connect<TMessage, TResult>(nextPipe, routingKey);
        return this;
    }
}

internal class Plumber<TMessage> : IPlumber<TMessage>
{
    private readonly IPipeConnector _pipeConnector;
    private readonly IPipeFactory _pipeFactory;

    public Plumber(IPipeConnector pipeConnector, IPipeFactory pipeFactory)
    {
        _pipeConnector = pipeConnector;
        _pipeFactory = pipeFactory;
    }

    public async Task<IPlumber<TMessage>> Connect<TPipe>(string routingKey = "",
        Action<IPlumber<TMessage>>? next = null)
        where TPipe : IPipe, IPipeConnector
    {
        var nextPipe = _pipeFactory.Create<TPipe>();
        var nextPlumber = new Plumber<TMessage>(nextPipe, _pipeFactory);
        next?.Invoke(nextPlumber);

        await _pipeConnector.Connect<TMessage>(nextPipe, routingKey);
        return this;
    }

    public async Task<IPlumber<TMessage>> ConnectHandlers(string routingKey = "",
        Action<IHandlerPlumber<TMessage>>? next = null)
    {
        var nextPipe = _pipeFactory.Create<HandlingPipe>();
        var nextPlumber = new HandlerPlumber<TMessage>(nextPipe);
        next?.Invoke(nextPlumber);

        await _pipeConnector.Connect<TMessage>(nextPipe, routingKey);
        return this;
    }
}

internal class HandlerPlumber<TMessage> : IHandlerPlumber<TMessage>
{
    private readonly HandlingPipe _pipe;

    public HandlerPlumber(HandlingPipe pipe)
    {
        _pipe = pipe;
    }

    public IHandlerPlumber<TMessage> Connect(Func<IServiceProvider, IHandler> factory, string routingKey = "")
    {
        _pipe.Connect<TMessage>(factory, routingKey);
        return this;
    }
}

internal class Plumber<TMessage, TResult> : IPlumber<TMessage, TResult>
{
    private readonly IPipeConnector _pipeConnector;
    private readonly IPipeFactory _pipeFactory;

    public Plumber(IPipeConnector pipeConnector, IPipeFactory pipeFactory)
    {
        _pipeConnector = pipeConnector;
        _pipeFactory = pipeFactory;
    }

    public async Task<IPlumber<TMessage, TResult>> Connect<TPipe>(string routingKey = "",
        Action<IPlumber<TMessage, TResult>>? next = null) where TPipe : IPipe, IPipeConnector
    {
        var nextPipe = _pipeFactory.Create<TPipe>();
        var nextPlumber = new Plumber<TMessage, TResult>(nextPipe, _pipeFactory);
        next?.Invoke(nextPlumber);

        await _pipeConnector.Connect<TMessage, TResult>(nextPipe, routingKey);
        return this;
    }

    public async Task<IPlumber<TMessage, TResult>> ConnectHandlers(string routingKey = "",
        Action<IHandlerPlumber<TMessage, TResult>>? next = null)
    {
        var nextPipe = _pipeFactory.Create<HandlingPipe>();
        var nextPlumber = new HandlerPlumber<TMessage, TResult>(nextPipe);
        next?.Invoke(nextPlumber);

        await _pipeConnector.Connect<TMessage, TResult>(nextPipe, routingKey);
        return this;
    }
}

internal class HandlerPlumber<TMessage, TResult> : IHandlerPlumber<TMessage, TResult>
{
    private readonly HandlingPipe _pipe;

    public HandlerPlumber(HandlingPipe pipe)
    {
        _pipe = pipe;
    }

    public IHandlerPlumber<TMessage, TResult> Connect(Func<IServiceProvider, IHandler> factory, string routingKey = "")
    {
        _pipe.Connect<TMessage, TResult>(factory, routingKey);
        return this;
    }
}