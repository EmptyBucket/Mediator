using FlexMediator.Pipes;

namespace FlexMediator;

internal class MediatorFactory : IMediatorFactory
{
    private readonly Lazy<Task<IMediator>> _mediator;

    public MediatorFactory(Func<IServiceProvider, IPipeConnector, Task> builder, IServiceProvider serviceProvider)
    {
        _mediator = new Lazy<Task<IMediator>>(async () =>
        {
            var mediator = new Mediator(serviceProvider);

            await builder(serviceProvider, mediator);

            return mediator;
        });
    }

    public async Task<IMediator> CreateAsync() => await _mediator.Value;
}