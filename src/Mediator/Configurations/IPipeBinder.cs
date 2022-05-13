namespace Mediator.Configurations;

public interface IPipeBinder
{
    IPipeBinder Bind(Type pipeType, Type pipeImplType, string pipeName = "");
}