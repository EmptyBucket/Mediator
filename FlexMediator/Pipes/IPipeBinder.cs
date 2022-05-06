namespace FlexMediator.Pipes;

public interface IPipeBinder
{
    Task<PipeBind> Bind<TMessage>(IPipe pipe, string routingKey = "");

    Task<PipeBind> Bind<TMessage, TResult>(IPipe pipe, string routingKey = "");
}