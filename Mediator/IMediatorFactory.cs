namespace Mediator;

public interface IMediatorFactory
{
    Task<IMediator> CreateAsync();
}