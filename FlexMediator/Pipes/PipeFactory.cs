using Microsoft.Extensions.DependencyInjection;

namespace FlexMediator.Pipes;

internal class PipeFactory : IPipeFactory
{
    private readonly IServiceProvider _serviceProvider;

    public PipeFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public TPipe Create<TPipe>() where TPipe : IPipe => _serviceProvider.GetRequiredService<TPipe>();
}