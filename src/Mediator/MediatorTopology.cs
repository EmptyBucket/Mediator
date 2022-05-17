using Mediator.Pipes;

namespace Mediator;

public class MediatorTopology : IAsyncDisposable
{
    public MediatorTopology(IConnectingPipe dispatch, IConnectingPipe receive)
    {
        Dispatch = dispatch;
        Receive = receive;
    }

    public void Deconstruct(out IConnectingPipe dispatch, out IConnectingPipe receive)
    {
        dispatch = Dispatch;
        receive = Receive;
    }

    public IConnectingPipe Dispatch { get; set; }

    public IConnectingPipe Receive { get; set; }
    
    public async ValueTask DisposeAsync()
    {
        await Dispatch.DisposeAsync();
        await Receive.DisposeAsync();
    }
}