namespace FlexMediator.Pipes;

public interface IPipeConnector
{
    Task<PipeConnection<TPipe>> Connect<TMessage, TPipe>(TPipe pipe, string routingKey = "")
        where TPipe : IPipe;

    Task<PipeConnection<TPipe>> Connect<TMessage, TResult, TPipe>(TPipe pipe, string routingKey = "")
        where TPipe : IPipe;
}