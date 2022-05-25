using Mediator.Pipes;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator;

internal class MediatorFactory : IMediatorFactory
{
    private readonly Lazy<Task<IMediator>> _mediator;

    public MediatorFactory(IServiceProvider serviceProvider,
        Func<IServiceProvider, MediatorTopology, Task> connectPipes)
    {
        _mediator = new Lazy<Task<IMediator>>(async () =>
        {
            var pipeFactory = serviceProvider.GetRequiredService<IPipeFactory>();
            var dispatchPipe = pipeFactory.Create<Pipe>();
            var receivePipe = pipeFactory.Create<Pipe>();
            var mediatorTopology = new MediatorTopology(dispatchPipe, receivePipe);
            await connectPipes(serviceProvider, mediatorTopology);
            var mediator = new Mediator(mediatorTopology);
            return mediator;
        });
    }

    public async Task<IMediator> CreateAsync() => await _mediator.Value;
}