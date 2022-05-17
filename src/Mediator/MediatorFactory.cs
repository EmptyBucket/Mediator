using Mediator.Pipes;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator;

internal class MediatorFactory : IMediatorFactory
{
    private readonly Lazy<Task<IMediator>> _mediator;

    public MediatorFactory(IServiceProvider serviceProvider,
        Func<IServiceProvider, IMediator, Task>? connectPipes = null)
    {
        connectPipes ??= (_, _) => Task.CompletedTask;

        _mediator = new Lazy<Task<IMediator>>(async () =>
        {
            var pipeFactory = serviceProvider.GetRequiredService<IPipeFactory>();
            var dispatchPipe = pipeFactory.Create<ConnectingPipe>();
            var receivePipe = pipeFactory.Create<ConnectingPipe>();
            var mediatorTopology = new MediatorTopology(dispatchPipe, receivePipe);
            var mediator = new Mediator(mediatorTopology);

            await connectPipes(serviceProvider, mediator);

            return mediator;
        });
    }

    public async Task<IMediator> CreateAsync() => await _mediator.Value;
}