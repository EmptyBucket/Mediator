namespace FlexMediator;

public interface IMediatorFactory
{
    Task<IMediator> CreateAsync();
}