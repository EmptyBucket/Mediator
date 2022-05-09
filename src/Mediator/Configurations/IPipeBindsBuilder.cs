namespace Mediator.Configurations;

public interface IPipeBindsBuilder
{
    IPipeBindsBuilder BindPipe(Type pipeType, string pipeName = "");
}