namespace Mediator.Configurations;

public interface IPipeBinder
{
    IPipeBinder BindPipe(Type pipeType, string pipeName = "");
}