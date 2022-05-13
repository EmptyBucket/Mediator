namespace Mediator.Pipes;

public interface IPipeFactory
{
    TPipe Create<TPipe>(string pipeName = "");
}