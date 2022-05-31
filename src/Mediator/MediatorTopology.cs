using Mediator.Pipes;

namespace Mediator;

public class MediatorTopology : IAsyncDisposable
{
    public MediatorTopology(IConnectingPipe dispatch, IConnectingPipe receive, IPipeFactory pipeFactory)
    {
        Dispatch = dispatch;
        Receive = receive;
        PipeFactory = pipeFactory;
    }

    public void Deconstruct(out IConnectingPipe dispatch, out IConnectingPipe receive)
    {
        dispatch = Dispatch;
        receive = Receive;
    }
    
    public void Deconstruct(out IConnectingPipe dispatch, out IConnectingPipe receive, out IPipeFactory pipeFactory)
    {
        dispatch = Dispatch;
        receive = Receive;
        pipeFactory = PipeFactory;
    }

    public IConnectingPipe Dispatch { get; set; }

    public IConnectingPipe Receive { get; set; }
    
    public IPipeFactory PipeFactory { get; }

    public async ValueTask DisposeAsync()
    {
        await Dispatch.DisposeAsync();
        await Receive.DisposeAsync();
    }
}