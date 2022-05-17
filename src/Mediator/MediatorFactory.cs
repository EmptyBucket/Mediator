using Mediator.Pipes;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator;

internal class MediatorFactory : IMediatorFactory
{
    private readonly Lazy<Task<IMediator>> _mediator;

    public MediatorFactory(Func<IServiceProvider, IMediator, Task> connectPipes, IServiceProvider serviceProvider)
    {
        _mediator = new Lazy<Task<IMediator>>(async () =>
        {
            var pipeFactory = serviceProvider.GetRequiredService<IPipeFactory>();
            var dispatchPipe = pipeFactory.Create<ConnectingPipe>();
            var receivePipe = pipeFactory.Create<ConnectingPipe>();
            var mediator = new Mediator(dispatchPipe, receivePipe);

            await connectPipes(serviceProvider, mediator);

            return mediator;
        });
    }

    public async Task<IMediator> CreateAsync() => await _mediator.Value;
}