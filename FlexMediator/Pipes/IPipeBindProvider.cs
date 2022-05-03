namespace FlexMediator.Pipes;

public interface IPipeBindProvider
{
    IEnumerable<PipeBind> GetBindings<TMessage>(string routingKey = "");

    IEnumerable<PipeBind> GetBindings<TMessage, TResult>(string routingKey = "");
}