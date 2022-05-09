namespace Mediator.Configurations;

public static class PipeBindsBuilderExtensions
{
    public static IPipeBindsBuilder BindPipe<TPipe>(this IPipeBindsBuilder pipeBindsBuilder, string pipeName = "") =>
        pipeBindsBuilder.BindPipe(typeof(TPipe), pipeName);
}