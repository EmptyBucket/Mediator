namespace Mediator;

public interface IPipeFactory
{
    TPipe Create<TPipe>(string pipeName = "");
}