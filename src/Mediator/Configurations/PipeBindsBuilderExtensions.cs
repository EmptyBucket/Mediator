namespace Mediator.Configurations;

public static class PipeBindsBuilderExtensions
{
    public static IPipeBinder BindPipe<TPipe>(this IPipeBinder pipeBinder, string pipeName = "") =>
        pipeBinder.BindPipe(typeof(TPipe), pipeName);
}