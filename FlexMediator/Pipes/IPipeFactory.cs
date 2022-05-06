namespace FlexMediator.Pipes;

public interface IPipeFactory
{
    TPipe Create<TPipe>() where TPipe : IPipe;
}